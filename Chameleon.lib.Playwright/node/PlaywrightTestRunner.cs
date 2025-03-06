using System.Diagnostics;
using System.Text.Json;
using Chameleon.lib.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chameleon.lib.Playwright.node;
public class PlaywrightTestRunner : IDisposable {
	private readonly TaskCompletionSource<bool> _tcs = new();

	readonly Func<string, Task<string>>? onAsk = null;
	readonly Process nodeProcess;
	readonly StreamWriter processInput;
	readonly string file;

	public event EventHandler<string>? TestOutputReceived;
	public event EventHandler<string>? TestErrorReceived;

	public PlaywrightTestRunner(string relativePath, Func<string, Task<string>>? onAsk = null) {
		this.onAsk = onAsk;
		file = relativePath;
		var nodePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
#if DEBUG
		".playwright"
#else
		OperatingSystem.IsWindows() ? ".playwright" : "../Resources/.playwright"
#endif
		, OperatingSystem.IsWindows() ? @"node\win32_x64\node.exe" : "node/darwin-x64/node");

		var args =
#if DEBUG
		OperatingSystem.IsWindows() ?
			@"C:\repos\chameleon-playwright\dist\bundle.js"
			: "/Users/dev/src/chameleon-playwright/dist/bundle.js"
#else
		Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
			OperatingSystem.IsWindows() ? 
				@"Resources\scripts\dist\bundle.js"
			 	: "../Resources/scripts/dist/bundle.js")
#endif
		;
		nodeProcess = new Process { 
			StartInfo = new ProcessStartInfo {
				FileName = OperatingSystem.IsWindows() ? $"\"{nodePath}\"" : nodePath,
				Arguments = OperatingSystem.IsWindows() ? $"\"{args}\"" : args,
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

	public async Task RunTestAsync(int port, object? options = null) {
		try {
			var command = new { arg = "run", file, port, options };
			var jsonCommand = JsonSerializer.Serialize(command);
			await processInput.WriteLineAsync(jsonCommand);
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
