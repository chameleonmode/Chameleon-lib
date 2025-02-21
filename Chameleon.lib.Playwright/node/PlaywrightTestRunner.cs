using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Chameleon.lib.Playwright.node;
public class PlaywrightTestRunner : IDisposable {
	private readonly TaskCompletionSource<bool> _tcs = new();

	private readonly Process _nodeProcess;
	private readonly StreamWriter _processInput;
	private readonly string _scriptName;

	public event EventHandler<string>? TestOutputReceived;
	public event EventHandler<string>? TestErrorReceived;

	public static PlaywrightTestRunner Create(string scriptName) {
		return new PlaywrightTestRunner(scriptName);
	}
	private PlaywrightTestRunner(string scriptName) {
		_scriptName = scriptName;
		var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
			#if DEBUG
				".playwright"
			#else
				OperatingSystem.IsWindows() ? @".playwright" :
				"../Resources/.playwright"
			#endif
			);

		var nodePath = Path.Combine(basePath,
			OperatingSystem.IsWindows() ? @"node\win32_x64\node.exe" : "node/darwin-x64/node");
		if (OperatingSystem.IsWindows())
			nodePath = @$"""{nodePath}""";

		var args =
#if DEBUG
		OperatingSystem.IsWindows() ?
			@"C:\repos\chameleon-playwright\dist\index.js"
			: "/Users/dev/src/chameleon-playwright/dist/index.js"
#else
		OperatingSystem.IsWindows() ?
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\scripts\dist\index.js")
			: Path.Combine(basePath, "scripts/dist/index.js")
#endif
		;
		if (OperatingSystem.IsWindows())
			args = @$"""{args}""";

		var startInfo = new ProcessStartInfo {
			FileName = nodePath,
			Arguments = args,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			//WorkingDirectory = Path.GetDirectoryName(nodePath) ?? throw new InvalidOperationException("Invalid node path")
		};

		_nodeProcess = new Process { StartInfo = startInfo };
		_nodeProcess.OutputDataReceived += (sender, e) => {
			var output = e.Data ?? string.Empty;
			Debug.WriteLine(output);
			TestOutputReceived?.Invoke(this, output);
			if (output == $"Test {scriptName} completed finally block")
				_ = _tcs.TrySetResult(true);
		};
		_nodeProcess.ErrorDataReceived += (sender, e) => {
			var output = e.Data ?? string.Empty;
			Debug.WriteLine(output);
			TestErrorReceived?.Invoke(this, e.Data ?? string.Empty);
			if (output.Contains("Error: Cannot find module"))
				_ = _tcs.TrySetResult(false);
		};

		_ = _nodeProcess.Start();
		_nodeProcess.BeginOutputReadLine();
		_nodeProcess.BeginErrorReadLine();

		_processInput = _nodeProcess.StandardInput;
	}

	public async Task RunTestAsync(object data, int port) {
		try {
			var command = new { action = "run", name = _scriptName, port, data };
			var jsonCommand = JsonSerializer.Serialize(command);
			await _processInput.WriteLineAsync(jsonCommand);
			_ = await _tcs.Task;
		} finally {
			await Task.Delay(1000);
		}
	}

	public async Task SetConfigurationAsync(string key, object value) {
		var command = new { action = "setConfig", key, value };
		var jsonCommand = JsonSerializer.Serialize(command);
		await _processInput.WriteLineAsync(jsonCommand);
	}

	public void Dispose() {
		try {
			_nodeProcess!.Kill();
			_nodeProcess!.Dispose();
		} catch (Exception e) {
			Console.WriteLine(e.Message);
		} finally {
			GC.SuppressFinalize(this);
		}
	}
}
