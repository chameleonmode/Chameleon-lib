using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Constants;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using System.Diagnostics;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance
		: ISysBrowserInstance {

	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public Process? Brocess { get; set; }
	public required SysBrowserSettings Settings { get; init; }

	public async Task InitializeAsync(object? param = null)
	{
		if (Brocess is null) {
			await InitializeExtensionPath();
			if (LoadedTCS.Task.IsCompleted)
				return;
			if (await StartProcess(GetCommandLineArguments()))
				_ = LoadedTCS.TrySetResult(true);
			else
				Close();
		}
	}
	public async Task InitializePrefsFile()
	{
		Toaster.ShowInf("Creating Prefs file for new profile cache wait for the browser window to relaunch a second time");
		TaskCompletionSource tcs = new();
		new Thread(async () => {
			try {
				using var p = ProUtil.Createa(ExePath, GetCommandLineArguments());
				_ = p.Start();
				await Task.Delay(1800);
				p.Exited += (sender, e) => {
					_ = tcs.TrySetResult();
				};

				_ = await TaskUtil.AwaitFor(() => {
					Thread.Sleep(256);		
					if(OperatingSystem.IsMacOS()) {
						if(MacOSUtil.FindWindowByPID(p.Id) == null)
							return false;
						// Use a shell command to send SIGTERM (graceful termination)
						using var killprocess = Process.Start("kill", $"-SIGTERM {p.Id}");
						// Wait for the process to exit
						_ = killprocess.WaitForExit(1);
					} else {
						// Attempt to close the browser gracefully
						_ = p.CloseMainWindow();
						_ = p.WaitForExit(TimeSpan.FromSeconds(1)); // Ensure the process has fully exited			
					}
					return p.HasExited || File.Exists(PrefsFile);
				}, 18, 36);
				
			  // Kill the process if it hasn't exited
				if (!p.HasExited) {
						p.Kill();
				}
				p.Dispose();
			} catch (Exception ex) {
				// Handle or log the exception as needed
				_ = tcs.TrySetException(ex);
			} finally {
				_ = tcs.TrySetResult();
			}
		}) {
			IsBackground = true,
		}.Start();

		await tcs.Task;
	}

	public void InvokeEvent(SysBrowserEventType eventType)
	{
		if (eventType == SysBrowserEventType.Foreground)
			_ = ProUtil.TrySetForeground(Brocess);

		OnEvent?.Invoke(this, new(Settings.OpenOptions, eventType));
	}

	public void Close()
	{
		if (OperatingSystem.IsMacOS()) {
			if (Brocess?.Id is int id)
				MacOSWindowListener.Instance.RemPid(id);
		}

		_ = LoadedTCS.TrySetResult(false);
		Brocess?.Dispose();
		Brocess = null;
		InvokeEvent(Enums.SysBrowserEventType.Closed);
	}

	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments();
	protected abstract Task<bool> StartProcess(string args);
}
