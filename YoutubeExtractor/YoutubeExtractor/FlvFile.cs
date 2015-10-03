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

using System;
using System.IO;

namespace YoutubeExtractor {
    internal class FlvFile : IDisposable {
        private readonly long fileLength;
        private readonly string inputPath;
        private readonly string outputPath;
        private IAudioExtractor audioExtractor;
        private long fileOffset;
        private FileStream fileStream;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FlvFile" /> class.
        /// </summary>
        /// <param name="inputPath">The path of the input.</param>
        /// <param name="outputPath">The path of the output without extension.</param>
        public FlvFile(string inputPath, string outputPath) {
            this.inputPath = inputPath;
            this.outputPath = outputPath;
            fileStream = new FileStream(this.inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
            fileOffset = 0;
            fileLength = fileStream.Length;
        }

        public bool ExtractedAudio { get; private set; }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event EventHandler<ProgressEventArgs> ConversionProgressChanged;

        /// <exception cref="AudioExtractionException">The input file is not an FLV file.</exception>
        public void ExtractStreams() {
            Seek(0);

            if (ReadUInt32() != 0x464C5601)
                // not a FLV file
                throw new AudioExtractionException("Invalid input file. Impossible to extract audio track.");

            ReadUInt8();
            var dataOffset = ReadUInt32();

            Seek(dataOffset);

            ReadUInt32();

            while (fileOffset < fileLength) {
                if (!ReadTag())
                    break;

                if (fileLength - fileOffset < 4)
                    break;

                ReadUInt32();

                var progress = (fileOffset * 1.0 / fileLength) * 100;

                if (ConversionProgressChanged != null)
                    ConversionProgressChanged(this, new ProgressEventArgs(progress));
            }

            CloseOutput(false);
        }

        private void CloseOutput(bool disposing) {
            if (audioExtractor != null) {
                if (disposing && audioExtractor.VideoPath != null)
                    try {
                        File.Delete(audioExtractor.VideoPath);
                    } catch {}

                audioExtractor.Dispose();
                audioExtractor = null;
            }
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                if (fileStream != null) {
                    fileStream.Close();
                    fileStream = null;
                }

                CloseOutput(true);
            }
        }

        private IAudioExtractor GetAudioWriter(uint mediaInfo) {
            var format = mediaInfo >> 4;

            switch (format) {
                case 14:
                case 2:
                    return new Mp3AudioExtractor(outputPath);

                case 10:
                    return new AacAudioExtractor(outputPath);
            }

            string typeStr;

            switch (format) {
                case 1:
                    typeStr = "ADPCM";
                    break;

                case 6:
                case 5:
                case 4:
                    typeStr = "Nellymoser";
                    break;

                default:
                    typeStr = "format=" + format;
                    break;
            }

            throw new AudioExtractionException("Unable to extract audio (" + typeStr + " is unsupported).");
        }

        private byte[] ReadBytes(int length) {
            var buff = new byte[length];

            fileStream.Read(buff, 0, length);
            fileOffset += length;

            return buff;
        }

        private bool ReadTag() {
            if (fileLength - fileOffset < 11)
                return false;

            // Read tag header
            var tagType = ReadUInt8();
            var dataSize = ReadUInt24();
            var timeStamp = ReadUInt24();
            timeStamp |= ReadUInt8() << 24;
            ReadUInt24();

            // Read tag data
            if (dataSize == 0)
                return true;

            if (fileLength - fileOffset < dataSize)
                return false;

            var mediaInfo = ReadUInt8();
            dataSize -= 1;
            var data = ReadBytes((int) dataSize);

            if (tagType == 0x8) {
                // If we have no audio writer, create one
                if (audioExtractor == null) {
                    audioExtractor = GetAudioWriter(mediaInfo);
                    ExtractedAudio = audioExtractor != null;
                }

                if (audioExtractor == null)
                    throw new InvalidOperationException("No supported audio writer found.");

                audioExtractor.WriteChunk(data, timeStamp);
            }

            return true;
        }

        private uint ReadUInt24() {
            var x = new byte[4];

            fileStream.Read(x, 1, 3);
            fileOffset += 3;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt32() {
            var x = new byte[4];

            fileStream.Read(x, 0, 4);
            fileOffset += 4;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt8() {
            fileOffset += 1;
            return (uint) fileStream.ReadByte();
        }

        private void Seek(long offset) {
            fileStream.Seek(offset, SeekOrigin.Begin);
            fileOffset = offset;
        }
    }
}