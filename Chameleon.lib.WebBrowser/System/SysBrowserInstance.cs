using System.Text;
using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util;
using Chameleon.lib.ThirdParty.GeoIp;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.ServiceManagers;
using Chameleon.lib.Common.Constants;
using static Chameleon.lib.Common.Constants.Enums;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

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

	public async Task<string> GetTimezone()
	{
		var timezone = "America/Los_Angeles";
		if (Settings.Emulation.AutoTimezone && Settings.Profile.Proxy.CanUse && Settings.Profile.Proxy.ServerForRequest.Is()) {
			try {
				var ipapi = await GeoIpApi.GetIpapi(Settings.Profile.Proxy.ServerForRequest!, e => Toaster.ShowErr(e),
			Settings.Profile.Proxy.UserName, Settings.Profile.Proxy.Password).ConfigureAwait(false);
				if (ipapi?.timezone != null) {
					timezone = ipapi.timezone;
				}
			} catch (Exception ex) {
				Toaster.ShowErr($"Request for timezone failed, {Settings.Profile.Proxy.Server} - {ex.Message}");
			}
		}
	
		return timezone;
	}

	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments();
}
