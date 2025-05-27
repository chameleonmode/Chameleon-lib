using System.Diagnostics;
using System.Text.Json;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Playwright.Services;
public class Runner : IDisposable {
	private readonly TaskCompletionSource<bool> _tcs = new();

	readonly Func<string, Task<string>>? onAsk = null;
	readonly Process nodeProcess;
	readonly StreamWriter processInput;

	public event EventHandler<string>? TestOutputReceived;
	public event EventHandler<string>? TestErrorReceived;

	public Runner(Func<string, Task<string>>? onAsk = null) {
		this.onAsk = onAsk;
		nodeProcess = new Process { 
			StartInfo = new ProcessStartInfo {
				FileName = OperatingSystem.IsWindows() ? $"\"{Project.Plugins.Node}\"" : Project.Plugins.Node,
				Arguments = $"\"{Project.Plugins.App}\"",
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
			if (output.StartsWith($"Catch:"))
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
		if (output.StartsWith("Try:") && output.EndsWith("success"))
			_ = _tcs.TrySetResult(true);
		else if (output.StartsWith("Ask:") && onAsk is not null)
			processInput?.WriteLine($"Answer:{await onAsk(output[3..])}");
	}

	public async Task Run(int port, string file, object? opts = null) {
		var command = new { arg = "run", file, port, opts };
		await Send(JS.Serialize(command));
	}

	public async Task Send(string options) {
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
