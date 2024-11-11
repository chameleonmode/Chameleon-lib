using System.Text;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Constants;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using Chameleon.lib.Common.Util.ThirdParty.GeoIp;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance
		: ISysBrowserInstance {

	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();

	public required SysBrowserSettings Settings { get; init; }

	public async Task InitializeAsync(object? param = null)
	{
		if (Settings.Brocess is null) {
			await InitializeExtensionPath();
			if (LoadedTCS.Task.IsCompleted)
				return;
			if (await Settings.StartProcess(GetCommandLineArguments(), Close))
				_ = LoadedTCS.TrySetResult(true);
			else
				Close();
		}
	}

	public void InvokeEvent(SysBrowserEventType eventType)
	{
		if (eventType == SysBrowserEventType.Foreground)
			_ = ProUtil.TrySetForeground(Settings.Brocess);

		OnEvent?.Invoke(this, new(Settings.OpenOptions, eventType));
	}

	public void Close()
	{
		if (OperatingSystem.IsMacOS()) {
			if (Settings.Brocess?.Id is int id)
				MacOSWindowListener.Instance.RemPid(id);
		}

		_ = LoadedTCS.TrySetResult(false);
		Settings.Brocess?.Dispose();
		Settings.Brocess = null;
		InvokeEvent(Enums.SysBrowserEventType.Closed);
	}

	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments();
}
