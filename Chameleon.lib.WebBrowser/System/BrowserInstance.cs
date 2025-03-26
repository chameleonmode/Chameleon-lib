using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Constants;
using System.Diagnostics;
using System.Runtime.Versioning;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Interfaces;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance
		: IBrowserInstance {
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	public Process? Brocess { get; set; }
	public required SysBrowserSettings Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public virtual Task Start() => Task.CompletedTask;

	public void InvokeEvent(Enums.SysBrowserEventType eventType) {
		if (eventType == Enums.SysBrowserEventType.Foreground)
			_ = ProUtil.TrySetForeground(Brocess);

		OnEvent?.Invoke(this, new(Settings.OpenOptions, eventType));
	}

	public void Close() {
		if (OperatingSystem.IsMacOS()) {
			if (Brocess?.Id is int id)
				MacOSWindowListener.Instance.RemPid(id);
		}

		_ = LoadedTCS.TrySetResult(false);
		Brocess?.Dispose();
		Brocess = null;
		InvokeEvent(Enums.SysBrowserEventType.Closed);
	}

	public async Task InitializeAsync(object? param = null) {
		if (Brocess is null) {
			await InitializeExtensionPath();
			if (LoadedTCS.Task.IsCompleted)
				return;

			// StartProcess
			Brocess = ProUtil.Createa(ExePath, GetCommandLineArguments());
			_ = Brocess.Start();
			await Task.Delay(1800);

			//
			if (OperatingSystem.IsMacOS()) {
				Brocess.Exited += (s, e) => { Close(); };

				if (await TaskUtil.AwaitFor(() =>
						Brocess?.HasExited == false && MacOSUtil.FindWindowByPID(Brocess.Id) != null, 36, 1000)
					) {
					MacOSWindowListener.Instance.AddPid(Brocess!.Id);
				}
			} else if (OperatingSystem.IsWindows()) {
				await WaitForWinHandle();
			}

			if (!Brocess.HasExited)
				_ = LoadedTCS.TrySetResult(true);
			else
				Close();
		}
	}

	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments();
	
	[SupportedOSPlatform("windows")]
	protected abstract Task WaitForWinHandle();

}
