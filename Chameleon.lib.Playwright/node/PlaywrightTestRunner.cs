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

	public static PlaywrightTestRunner Create(string scriptName)
	{
		return new PlaywrightTestRunner(
#if DEBUG 
		OperatingSystem.IsMacOS() ? 
		"/Users/dev/src/Chameleon"
		: "C:\\repos\\Chameleon\\Chameleon.Avalonia\\src\\Chameleon.Avalonia.Desktop\\obj\\outwin"
#endif
		, scriptName);
	}
	private PlaywrightTestRunner(string? basePath, string scriptName)
	{
		_scriptName = scriptName;

		basePath = Path.Combine(basePath ?? AppDomain.CurrentDomain.BaseDirectory,
		 OperatingSystem.IsMacOS() ?
		  "../Resources/.playwright"
			: ".playwright");

		var nodePath = Path.Combine(basePath, OperatingSystem.IsMacOS()
				? "node/darwin-x64/node"
				: "node\\win32_x64\\node.exe");
		if (!OperatingSystem.IsMacOS())
			nodePath = @$"""{nodePath}""";

		var args = Path.Combine(basePath, OperatingSystem.IsMacOS()
				? "scripts/dist/index.js"
				: "scripts\\dist\\index.js");
		if (!OperatingSystem.IsMacOS())
			args = @$"""{args}""";

		var startInfo = new ProcessStartInfo {
			FileName = nodePath,
			Arguments = args,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
      WorkingDirectory = Path.GetDirectoryName(nodePath) ?? throw new InvalidOperationException("Invalid node path")
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

	public async Task RunTestAsync(object data, int port)
	{
		try {
			var command = new { action = "run", name = _scriptName, port, data };
			var jsonCommand = JsonSerializer.Serialize(command);
			await _processInput.WriteLineAsync(jsonCommand);
			_ = await _tcs.Task;
		} finally {
			await Task.Delay(1000);
		}
	}

	public async Task SetConfigurationAsync(string key, object value)
	{
		var command = new { action = "setConfig", key, value };
		var jsonCommand = JsonSerializer.Serialize(command);
		await _processInput.WriteLineAsync(jsonCommand);
	}

	public void Dispose()
	{
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
