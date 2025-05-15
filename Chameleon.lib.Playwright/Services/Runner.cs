using System.Diagnostics;
using System.Text.Json;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Playwright.node;
public class Runner : IDisposable {
	private readonly TaskCompletionSource<bool> _tcs = new();

	readonly Func<string, Task<string>>? onAsk = null;
	readonly Process nodeProcess;
	readonly StreamWriter processInput;
	readonly string file;

	public event EventHandler<string>? TestOutputReceived;
	public event EventHandler<string>? TestErrorReceived;

	public Runner(string relativePath, Func<string, Task<string>>? onAsk = null) {
		this.onAsk = onAsk;
		file = relativePath;
		var nodePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
#if DEBUG
		".playwright"
#else
		OperatingSystem.IsWindows() ? ".playwright" : "../Resources/.playwright"
#endif
		, OperatingSystem.IsWindows() ? @"node\win32_x64\node.exe" : "node/darwin-x64/node");

		// TODO:
		// var director = Path.Combine(FilePaths.Playwright, "app.js");
		var args =
#if DEBUG
		OperatingSystem.IsWindows() 
			? @"C:\repos\chameleon-playwright\dist\app.js"
			: "/Users/dev/src/chameleon-playwright/dist/app.js";
#else
		Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			OperatingSystem.IsWindows() 
			?	@"Resources\scripts\dist\app.js"
			: "../Resources/scripts/dist/app.js"
		);
#endif
		nodeProcess = new Process { 
			StartInfo = new ProcessStartInfo {
				FileName = OperatingSystem.IsWindows() ? $"\"{nodePath}\"" : nodePath,
				Arguments = $"\"{args}\"",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			} 
		};
		nodeProcess.OutputDataReceived += OnOutputDataReceived;
		nodeProcess.ErrorDataReceived += (sender, e) => {
			var output = e.Data ?? string.Empty;
			Debug.WriteLine(output);
			TestErrorReceived?.Invoke(this, e.Data ?? string.Empty);
			if (output.StartsWith($"Catch: {file}"))
				_ = _tcs.TrySetResult(false);
		};

		_ = nodeProcess.Start();
		nodeProcess.BeginOutputReadLine();
		nodeProcess.BeginErrorReadLine();

		processInput = nodeProcess.StandardInput;
	}
	public async void OnOutputDataReceived(object sender, DataReceivedEventArgs e) {
		var output = e.Data ?? string.Empty;
		Debug.WriteLine(output);
		TestOutputReceived?.Invoke(this, output);
		if (output == $"Try: {file} success")
			_ = _tcs.TrySetResult(true);

		if (output.StartsWith("Ask:") && onAsk is not null) {
			processInput?.WriteLine($"Answer:{await onAsk(output[3..])}");
		}
	}

	public async Task Run(int port, object? options = null) {
		var command = new { arg = "run", file, port, options };
		await Run(JS.Serialize(command));
	}

	public async Task Run(string options) {
		try {
			await processInput.WriteLineAsync(options);
			_ = await _tcs.Task;
		} finally {
			await Task.Delay(1000);
		}
	}

	public async Task SetConfigurationAsync(string key, object value) {
		var command = new { action = "setConfig", key, value };
		var jsonCommand = JsonSerializer.Serialize(command);
		await processInput.WriteLineAsync(jsonCommand);
	}

	public void Dispose() {
		try {
			processInput.WriteLine("exit");
			nodeProcess?.Kill();
			nodeProcess?.Dispose();
		} catch (Exception e) {
			Toaster.Error(e.Message);
		} finally {
			GC.SuppressFinalize(this);
		}
	}
}
