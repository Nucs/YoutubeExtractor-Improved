# YoutubeExtractor - MP4 Audio Extraction Support
This fork adds support for converting MP4 to AAC file 
using `MediaToolkit C# Library` - a wrapper for `ffmpeg C91 Library`.

The AAC audio file extraction from a MP4 file is done the same way as before (using `AudioDownloader`).

#### Main Changes:
- `.NET Framework` has been upgraded to 4.6.
- `Newtonsoft.JSON` has been upgraded to 7.0.1.
- Changed the system to a `context-based` extraction, see examples or unit tests.


#### Target platforms
    Confirmed to work on a desktop application under Win7.

## Example for simplified usages

**Context Initiating**

```c#
//Simple initializing
var link = "https://www.youtube.com/watch?v=Q7ajZiT1Yms";
var context = new YoutubeContext(link);

//Defining download directory
var context = new YoutubeContext(Url) 
    {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
//The file name is automatically set by the video's title
//and can later be accessible from context.AudioPath
```

---

**Finding highest quality audio and downloading it**
```c#
string url = "https://www.youtube.com/watch?v=Q7ajZiT1Yms";
//init a simple context to temp dir.
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
//Finds the best quality audio and sets it to the context
DownloadUrlResolver.FindHighestAudioQualityDownloadUrl(yc);

//Init a AudioDownloader with the current context and the rest is magic.
var ad = new AudioDownloader(yc);
ad.Execute();
Console.WriteLine(yc.AudioPath.FullName);
yc.AudioPath.Delete();
```
Async
```c#
string url = "https://www.youtube.com/watch?v=Q7ajZiT1Yms";
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
await DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(yc);
var ad = new AudioDownloader(yc);
await ad.ExecuteAsync();
Console.WriteLine(yc.AudioPath.FullName);
yc.AudioPath.Delete();
```
---
**Short way to finding highest quality audio and downloading it**
```c#
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
var ad = new AudioDownloader(yc, true);
ad.Execute();
```

Async
```c#
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
await DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(yc);
var ad = new AudioDownloader(yc);
await ad.ExecuteAsync();
```
---
**Short way to finding highest quality video and downloading it**
```c#
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
var vd = new VideoDownloader(yc, true);
vd.Execute();
```
Async
```c#
var yc = new YoutubeContext(url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
await DownloadUrlResolver.FindHighestVideoQualityDownloadUrlAsync(yc);
var ad = new VideoDownloader(yc);
await ad.ExecuteAsync();
```
---

### Stage Changed Handling
```c#
context.ProgresStateChanged += (sender, args) => {
    switch (args.Stage) {
        case YoutubeStage.ProcessingUrls:
            ReportProgress(context, "Processing Urls");
            break;
        case YoutubeStage.DecipheringUrls:
            ReportProgress(context, "Deciphering Urls");
            break;
        case YoutubeStage.StartingDownload:
            ReportProgress(context, "Download Starting");
            break;
        case YoutubeStage.Downloading:
            ReportProgress(context, $"{args.Precentage.ToString("F1")}%");
            break;
        case YoutubeStage.DownloadFinished:
            ReportProgress(context, "Download Finished");
            break;
        case YoutubeStage.StartingAudioExtraction:
            ReportProgress(context, "Starting Extracting Audio");
            break;
        case YoutubeStage.ExtractingAudio:
            ReportProgress(context, $"{args.Precentage.ToString("F1")}%");
            break;
        case YoutubeStage.FinishedAudioExtraction:
            ReportProgress(context, "Finished Extracting Audio");
            break;
        case YoutubeStage.Completed:
            ReportProgress(context, "Completed");
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }
};

public void ReportProgress(YoutubeContext context, string stage) {
    //do something with the stage change
}
```

### Error Handling
The error handling is managed through event in the context `context.DownloadFailed`.
It will catch any web request fails, but not null reference exceptions and so on.
When a web request failed it will pass a `RetryableProcessFailed` object which has a flag to stop retrying based on your decision.
If not handled at all, it will retry infinitly.
```c#
yc.DownloadFailed += (sender, args) => {
    if (args.NumberOfTries >= 5)
        args.ShouldRetry = false;
    Console.WriteLine("args.Subject\n"+args.Exception);
};
```