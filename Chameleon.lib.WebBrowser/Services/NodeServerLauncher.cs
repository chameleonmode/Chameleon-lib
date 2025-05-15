using System.Diagnostics;
using System.Net.Http.Json;
using chameleon.assets;
using Chameleon.lib.Const;

namespace Chameleon.lib.WebBrowser.Services;
public class NodeServerLauncher {
  NodeServerLauncher() {
    nodeServerPath = Path.Combine(
      AppDomain.CurrentDomain.BaseDirectory,
#if DEBUG
        ".playwright",
#else
		    OperatingSystem.IsWindows() ? ".playwright" : "../Resources/.playwright",
#endif
      OperatingSystem.IsWindows() ? @"node\win32_x64\node.exe" : "node/darwin-x64/node"
    );

    serverJsDirPath = 
#if DEBUG
      "/Users/dev/src/chameleon-cli";
#else
      Path.Combine(FilePaths.AppDataLocalDir, "node");
#endif

    serverJsPath = Path.Combine(serverJsDirPath, "server.cjs");
  }
  readonly string nodeServerPath;
  readonly string serverJsDirPath;
  readonly string serverJsPath;
  readonly string url = $"http://127.0.0.1:3663/csharp/data";

  Process? node;
  public async Task StartServer() {
    if(node != null) return;

    await Load.Directory("js.node", serverJsDirPath);
    node = Process.Start(new ProcessStartInfo {
      FileName = $"\"{nodeServerPath}\"",
      Arguments = $"\"{serverJsPath}\"",
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    });
    node!.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
    node.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
    node.BeginOutputReadLine();
    node.BeginErrorReadLine();
  }

  // Send command
  public async Task SendLine(string command, object data) {
    var jsonCommand = JS.Serialize(new { command, data });
    await node!.StandardInput.WriteLineAsync(jsonCommand);
  }

	// POST request
  public async Task PostMessage(object data) {
		using var client = new HttpClient();
		var response = await client.PostAsync(url, JsonContent.Create(data, mediaType: null, JS.InsensitiveCamelCaseOptions));
		var responseBody = await response.Content.ReadAsStringAsync();
		Console.WriteLine($"Response: {responseBody}");
	}

  public void Dispose() {
    if (node != null) {
      node.StandardInput.WriteLine("exit");
      node.Kill();
      node.Dispose();
      node = null;
    }
  }

  // Singleton
  public static NodeServerLauncher Instance { get; } = new();
}
