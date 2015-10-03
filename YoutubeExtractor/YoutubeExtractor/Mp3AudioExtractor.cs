// ****************************************************************************
//
// FLV Extract
// Copyright (C) 2006-2012  J.D. Purcell (moitah@yahoo.com)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// ****************************************************************************

using System.Collections.Generic;
using System.IO;

namespace YoutubeExtractor {
    internal class Mp3AudioExtractor : IAudioExtractor {
        private readonly List<byte[]> chunkBuffer;
        private readonly FileStream fileStream;
        private readonly List<uint> frameOffsets;
        private readonly List<string> warnings;
        private int channelMode;
        private bool delayWrite;
        private int firstBitRate;
        private uint firstFrameHeader;
        private bool hasVbrHeader;
        private bool isVbr;
        private int mpegVersion;
        private int sampleRate;
        private uint totalFrameLength;
        private bool writeVbrHeader;

        public Mp3AudioExtractor(string path) {
            VideoPath = path;
            fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024);
            warnings = new List<string>();
            chunkBuffer = new List<byte[]>();
            frameOffsets = new List<uint>();
            delayWrite = true;
        }

        public IEnumerable<string> Warnings {
            get { return warnings; }
        }

        public string VideoPath { get; }

        public void Dispose() {
            Flush();

            if (writeVbrHeader) {
                fileStream.Seek(0, SeekOrigin.Begin);
                WriteVbrHeader(false);
            }

            fileStream.Dispose();
        }

        public void WriteChunk(byte[] chunk, uint timeStamp) {
            chunkBuffer.Add(chunk);
            ParseMp3Frames(chunk);

            if (delayWrite && totalFrameLength >= 65536)
                delayWrite = false;

            if (!delayWrite)
                Flush();
        }

        private static int GetFrameDataOffset(int mpegVersion, int channelMode) {
            return 4 + (mpegVersion == 3 ?
                (channelMode == 3 ? 17 : 32) :
                (channelMode == 3 ? 9 : 17));
        }

        private static int GetFrameLength(int mpegVersion, int bitRate, int sampleRate, int padding) {
            return (mpegVersion == 3 ? 144 : 72) * bitRate / sampleRate + padding;
        }

        private void Flush() {
            foreach (var chunk in chunkBuffer)
                fileStream.Write(chunk, 0, chunk.Length);

            chunkBuffer.Clear();
        }

        private void ParseMp3Frames(byte[] buffer) {
            var mpeg1BitRate = new[] {0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0};
            var mpeg2XBitRate = new[] {0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0};
            var mpeg1SampleRate = new[] {44100, 48000, 32000, 0};
            var mpeg20SampleRate = new[] {22050, 24000, 16000, 0};
            var mpeg25SampleRate = new[] {11025, 12000, 8000, 0};

            var offset = 0;
            var length = buffer.Length;

            while (length >= 4) {
                int mpegVersion, sampleRate, channelMode;

                var header = (ulong) BigEndianBitConverter.ToUInt32(buffer, offset) << 32;

                if (BitHelper.Read(ref header, 11) != 0x7FF)
                    break;

                mpegVersion = BitHelper.Read(ref header, 2);
                var layer = BitHelper.Read(ref header, 2);
                BitHelper.Read(ref header, 1);
                var bitRate = BitHelper.Read(ref header, 4);
                sampleRate = BitHelper.Read(ref header, 2);
                var padding = BitHelper.Read(ref header, 1);
                BitHelper.Read(ref header, 1);
                channelMode = BitHelper.Read(ref header, 2);

                if (mpegVersion == 1 || layer != 1 || bitRate == 0 || bitRate == 15 || sampleRate == 3)
                    break;

                bitRate = (mpegVersion == 3 ? mpeg1BitRate[bitRate] : mpeg2XBitRate[bitRate]) * 1000;

                switch (mpegVersion) {
                    case 2:
                        sampleRate = mpeg20SampleRate[sampleRate];
                        break;

                    case 3:
                        sampleRate = mpeg1SampleRate[sampleRate];
                        break;

                    default:
                        sampleRate = mpeg25SampleRate[sampleRate];
                        break;
                }

                var frameLenght = GetFrameLength(mpegVersion, bitRate, sampleRate, padding);

                if (frameLenght > length)
                    break;

                var isVbrHeaderFrame = false;

                if (frameOffsets.Count == 0) {
                    // Check for an existing VBR header just to be safe (I haven't seen any in FLVs)
                    var o = offset + GetFrameDataOffset(mpegVersion, channelMode);

                    if (BigEndianBitConverter.ToUInt32(buffer, o) == 0x58696E67) {
                        // "Xing"
                        isVbrHeaderFrame = true;
                        delayWrite = false;
                        hasVbrHeader = true;
                    }
                }

                if (!isVbrHeaderFrame)
                    if (firstBitRate == 0) {
                        firstBitRate = bitRate;
                        this.mpegVersion = mpegVersion;
                        this.sampleRate = sampleRate;
                        this.channelMode = channelMode;
                        firstFrameHeader = BigEndianBitConverter.ToUInt32(buffer, offset);
                    } else if (!isVbr && bitRate != firstBitRate) {
                        isVbr = true;

                        if (!hasVbrHeader)
                            if (delayWrite) {
                                WriteVbrHeader(true);
                                writeVbrHeader = true;
                                delayWrite = false;
                            } else
                                warnings.Add("Detected VBR too late, cannot add VBR header.");
                    }

                frameOffsets.Add(totalFrameLength + (uint) offset);

                offset += frameLenght;
                length -= frameLenght;
            }

            totalFrameLength += (uint) buffer.Length;
        }

        private void WriteVbrHeader(bool isPlaceholder) {
            var buffer = new byte[GetFrameLength(mpegVersion, 64000, sampleRate, 0)];

            if (!isPlaceholder) {
                var header = firstFrameHeader;
                var dataOffset = GetFrameDataOffset(mpegVersion, channelMode);
                header &= 0xFFFE0DFF; // Clear CRC, bitrate, and padding fields
                header |= (uint) (mpegVersion == 3 ? 5 : 8) << 12; // 64 kbit/sec
                BitHelper.CopyBytes(buffer, 0, BigEndianBitConverter.GetBytes(header));
                BitHelper.CopyBytes(buffer, dataOffset, BigEndianBitConverter.GetBytes(0x58696E67)); // "Xing"
                BitHelper.CopyBytes(buffer, dataOffset + 4, BigEndianBitConverter.GetBytes((uint) 0x7)); // Flags
                BitHelper.CopyBytes(buffer, dataOffset + 8, BigEndianBitConverter.GetBytes((uint) frameOffsets.Count)); // Frame count
                BitHelper.CopyBytes(buffer, dataOffset + 12, BigEndianBitConverter.GetBytes(totalFrameLength)); // File length

                for (var i = 0; i < 100; i++) {
                    var frameIndex = (int) ((i / 100.0) * frameOffsets.Count);

                    buffer[dataOffset + 16 + i] = (byte) (frameOffsets[frameIndex] / (double) totalFrameLength * 256.0);
                }
            }

            fileStream.Write(buffer, 0, buffer.Length);
        }
    }
}