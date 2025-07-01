using Chameleon.lib.AIR.Actors;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;

public class Service : Web {
  public static class Routes {
    public class App() : Root("app") {
      public static App Instance { get; } = new();
      public record AppClientInfo(string Latest);
      public static Task<AppClientInfo?> GetLatestVersion => Get<AppClientInfo>($"{Instance.Prefix}/latest",
        new(Q: $"?os={(OperatingSystem.IsMacOS() ? "mac" : "win")}", Authenticate: false)
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

        ProcessUtil.OpenFolder(FilePaths.AppDownloadDir);

        return File.Exists(outputFile);
      }
    }

    public class Air() : Root("air") {
      public static Air Instance { get; } = new();
      public record Response<T>(T Payload);
      public static readonly string[] backgrounds = ["sarcastic", "informative", "relatable", "straightforward"];

      public record AskRequest(string Feature, object Scenario, string? Background = null);
      public record AskResponse(string Response);
      public static Task<Response<AskResponse>?> Ask(AskRequest request) {
        return Post<Response<AskResponse>>($"{Instance.Prefix}/ask/gpt", new(
          Q: $"?feature={Uri.EscapeDataString(request.Feature)}",
          Body: new {
            background = request.Background ?? backgrounds[new Random().Next(0, backgrounds.Length)],
            scenario = request.Scenario
          }
        ));
      }
    }

    public class Promptee() : Root("robo") {
      public static Promptee Instance { get; } = new();
      public record Rep<T>(T Reply);
      public record GenorateRequest(Decorations Decorators, int Variations, IEnumerable<string> Search);
      public record GenorateResponse(string Type, string[] Data, object? Id, object? Reason);
      public static Task<Rep<IEnumerable<GenorateResponse>>?> Genorate(GenorateRequest request) {
        var ranger = request.Search.Count() + request.Variations;
        return Post<Rep<IEnumerable<GenorateResponse>>>($"{Instance.Prefix}/genorate", new(
          Headers: new() { { "ai", "origato" }, { "model", "o4-mini" } },
          Authenticate: false,
          Body: new {
            decorators = request.Decorators,
            task = "generate_search_terms",
            generations = new {
              type = "term",
              range = new { min = ranger, max = ranger },
              input = new {
                type = "search",
                data = request.Search,
                user_intent = $"generate variations of each search term as Singular Concepts",
              },
            },
          }
        ));
      }
    }
  }

  // Singleton
  public static Service Instance { get; } = new();
}
