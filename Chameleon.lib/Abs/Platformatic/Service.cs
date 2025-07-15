using Chameleon.lib.AIR.Actors;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;

public class Service : Web {
  public Routes.Roboto Robo { get; } = new();
  public static class Routes {
    public class App() : Root("app") {
      public static App Instance { get; } = new();
      public record AppClientInfo(string Latest);
      public static Task<AppClientInfo?> GetLatestVersion => Get<AppClientInfo>(
        new($"{Instance.Prefix}/latest", Q: $"?os={(OperatingSystem.IsMacOS() ? "mac" : "win")}", Authenticate: false)
      );
      public static async Task<bool> DownloadLatest(Action<string> onProgress) {
        // Local path where the downloaded file will be saved
        var ext = OperatingSystem.IsMacOS() ? "zip" : "7z";
        using var client = await Abs.HttpClient();
        // Send an asynchronous GET request and ensure headers are read before downloading the stream
        using var response = await client.GetAsync($"{Instance.Prefix}/download" + $"?ext={ext}", HttpCompletionOption.ResponseHeadersRead);
        _ = response.EnsureSuccessStatusCode();

        // Get the file name from the Content-Disposition header
        var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "Chameleon." + ext;
        var outputFile = Path.Combine(FilePaths.AppDownloadDir, fileName);

        // Get the total number of bytes (if available)
        var totalBytes = response.Content.Headers.ContentLength;
        var buffer = new byte[8192];
        double lastProgressReported = 0; // Tracks the last reported progress percentage
        long totalBytesRead = 0;
        int bytesRead;

        // Open a stream to write the downloaded content to a file
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

        // Read the content stream in chunks
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0) {
          // Write the chunk to the file
          await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
          totalBytesRead += bytesRead;

          // Report progress only if totalBytes is available and we've passed the next 10% increment.
          if (totalBytes.HasValue) {
            var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
            if (progressPercentage - lastProgressReported >= 10 || progressPercentage >= 100) {
              lastProgressReported = Math.Floor(progressPercentage / 10) * 10;
              var progress = $"Downloaded {totalBytesRead} of {totalBytes.Value} bytes ({progressPercentage:0.00}%)";
              onProgress(progress);
            }
          } else {
            // If total size is unknown, report the raw byte count (or customize as needed)
            onProgress($"Downloaded {totalBytesRead} bytes");
          }
        }

        FilePaths.OpenFolder(FilePaths.AppDownloadDir);

        return File.Exists(outputFile);
      }
    }

    public class Roboto() : Root("robo") {
      public record Rep<T>(T Reply);
      public record GenorateRequest(Decorations Decorators, int Variations, IEnumerable<string> Search);
      public record GenorateResponse(string Type, string[] Data, object? Id, object? Reason);
      public Task<Rep<IEnumerable<GenorateResponse>>?> Genorate(GenorateRequest request) {
        return Post<Rep<IEnumerable<GenorateResponse>>>(new(
          $"{Prefix}/terms",
          Headers: new() { { "ai", "origato" }, { "model", "o4-mini" } },
          Authenticate: false,
          Body: new {
            decorators = request.Decorators,
            task = $"suggest {(
              request.Variations)} variations of each of the terms in the input data array. the data in each reply should be a string array of {(
              request.Variations)} length",
            generations = new {
              type = "term",
              range = new { min = request.Search.Count(), max = request.Search.Count() },
              input = new {
                type = "search",
                data = request.Search,
                user_intent = $"consider each of these terms as a batch of distinct meanings unless otherwise specified",
              },
            },
          }
        ));
      }
      public async Task<IEnumerable<string>> Terms(GenorateRequest request, int tries = 3) {
        Toaster.Info($"Tries left {tries} to generate {request.Variations} variation{(
            request.Variations > 1 ? "s" : "" )} for {request.Search.Count()} term{(
            request.Search.Count() > 1 ? "s" : "")}");

        var response = await Genorate(request);
        return response is not null
           ? response.Reply.SelectMany(i => i.Data.Select(t => t.Trim()).Where(t => t.IsNot()))
           : tries > 0 ? await Terms(request, tries - 1) : [];
      }
    }
  }

  // Singleton
  public static Service I { get; } = new();
}
