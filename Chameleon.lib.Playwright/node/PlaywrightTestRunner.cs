using Chameleon.lib.Common;
using Chameleon.lib.Common.Interfaces;

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chameleon.lib.Playwright.node;
public class PlaywrightTestRunner : IDisposable {
	private readonly IChaonfigurationManager _configManager;
	private readonly Process _nodeProcess;
	private readonly StreamWriter _processInput;

	public event EventHandler<string>? TestOutputReceived;
	public event EventHandler<string>? TestErrorReceived;

	public PlaywrightTestRunner(IChaonfigurationManager configManager) {
		_configManager = configManager;

		var nodePath = _configManager.GetValue<string>("NodePath");
		var scriptPath = _configManager.GetValue<string>("ScriptPath");

		var startInfo = new ProcessStartInfo {
			FileName = nodePath,
			Arguments = scriptPath,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = Path.GetDirectoryName(scriptPath) // Set the working directory
		};

		_nodeProcess = new Process { StartInfo = startInfo };
		_nodeProcess.OutputDataReceived += (sender, e) => TestOutputReceived?.Invoke(this, e.Data ?? string.Empty);
		_nodeProcess.ErrorDataReceived += (sender, e) => TestErrorReceived?.Invoke(this, e.Data ?? string.Empty);

		_ = _nodeProcess.Start();
		_nodeProcess.BeginOutputReadLine();
		_nodeProcess.BeginErrorReadLine();

		_processInput = _nodeProcess.StandardInput;
	}

	public async Task RunTestAsync(string testName, object testData) {
		var command = new { action = "run", name = testName, port = 9669, data = testData };
		var jsonCommand = JsonSerializer.Serialize(command);
		await _processInput.WriteLineAsync(jsonCommand);
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
