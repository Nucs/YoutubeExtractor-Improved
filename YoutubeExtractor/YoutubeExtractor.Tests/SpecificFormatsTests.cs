/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YoutubeExtractor.Tests {
    [TestClass]
    public class SpecificFormatsTests {
        [TestMethod]
        public void Formats249250251() {
            //vid that is verified to have those formats
            var newvid = "https://www.youtube.com/watch?v=uxpDa-c-4Mc";
            var context = new YoutubeContext(newvid);
            var vidi = DownloadUrlResolver.GetDownloadUrls(context)
                .Where(vi => vi.FormatCode >= 249 && vi.FormatCode <= 251)
                .Where(vi => vi.VideoType == VideoType.Unknown)
                .OrderByDescending(info => info.AudioBitrate)
                .First();
            context.VideoInfo = vidi;

            var ad = new AudioDownloader(context);

            ad.DeleteVideoAfterExtract = true;

            ad.Execute();

            Assert.IsTrue(File.Exists(context.AudioPath.FullName));
        }

        [TestMethod]
        public void Wow() {
            //vid that is verified to have those formats
            var l = new List<Task>();
            var c = @"C:\extracted\convs";
            var newvid = "https://www.youtube.com/watch?v=YQHsXMglC9A";
            var vidi = DownloadUrlResolver.GetDownloadUrls(newvid).ToArray();
            foreach (var v in vidi) {
                var context = new YoutubeContext(newvid);
                context.VideoInfo = v;
                context.VideoPath = new FileInfo(Path.Combine(c, $"{v.FormatCode}.{v.AudioBitrate}{v.VideoExtension??".unknown"}"));
                var ad = new VideoDownloader(context);
                l.Add(ad.ExecuteAsync());
            }
            Task.WaitAll(l.ToArray());

        }
        
        [TestMethod]
        public void Wow2() {
            //vid that is verified to have those formats
            var l = new List<MediaFile>();
            var c = @"C:\extracted\convs";
            var tar = @"C:\extracted\convs\sorted\";
            var e = new Engine();
            
            foreach (var f in Directory.GetFiles(c)) {
                MediaFile mf;
                l.Add(mf = new MediaFile(f));
                try {
                    e.GetMetadata(mf);

                } catch (Exception ee) {
                    File.Copy(f,Path.Combine(tar,Path.GetFileName(f)));
                    File.WriteAllText(Path.Combine(tar, Path.GetFileName(f)+".txt"), ee.ToString());
                    continue;
                }
                if (mf.Metadata.AudioData == null)
                    continue;
                //var vidinfo = (mf.Metadata.VideoData?.Format ?? "") + "." + (mf.Metadata.VideoData?.FrameSize ?? "");
                //var audinfo = (mf.Metadata.AudioData?.Format ?? "") + "." + (mf.Metadata.AudioData?.BitRateKbs ?? 0) + "." + (mf.Metadata.AudioData?.SampleRate ?? "");
                //var name = $"{Path.GetFileNameWithoutExtension(f)}TT{vidinfo}TT{audinfo}".Replace("/","");
                var name = $"{Path.GetFileNameWithoutExtension(f)}.{mf.Metadata.AudioData.BitRateKbs}.{Path.GetExtension(f)}".Replace("/","");
                File.Copy(f, Path.Combine(tar, name));
            }
            e.Dispose();

        }
        [TestMethod]
        public void ConvertOGGtoAAC() {
            var @in = @"C:\extracted\251.opus".Replace("\\", "/");
            var @out = @"C:\extracted\converted.aac".Replace("\\","/");

            var _in = new MediaFile(@in);
            var _out = new MediaFile(@out);
            using (var engine = new Engine()) {
                engine.Convert(_in, _out);
            }


            Assert.IsTrue(File.Exists(@out), "No Output");
            Assert.IsTrue(new FileInfo(@out).Length > 0, "File is empty");
        }
    }
}

//engine.CustomCommand($"ffmpeg -i {@in} -c:a aac {@out}");
//engine.CustomCommand($"ffmpeg -i {@in} -c:a libvo_aacenc -b:a 128k {@out}");
//engine.CustomCommand($"ffmpeg -i {@in} -acodec libfaac {@out}");
//engine.CustomCommand($"ffmpeg -i {@in} -strict experimental -acodec aac {@out}");*/