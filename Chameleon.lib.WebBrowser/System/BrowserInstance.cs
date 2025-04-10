using Chameleon.lib.Common.Util;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Constants;
using System.Diagnostics;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.Helpers;
using Chameleon.lib.WebBrowser.Services;
using Chameleon.lib.Common.Util.ThirdParty.GeoIp;
using Chameleon.lib.Common.Util.Win;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SysBrowserInstance : IBrowserInstance {
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();
	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	public Process? Brocess { get; set; }
	public required SysBrowserSettings Settings { get; init; }
	public string SessionId { get; } = Guid.NewGuid().ToString();

	public void InvokeEvent(Enums.SysBrowserEventType eventType) {
		if (eventType == Enums.SysBrowserEventType.Foreground && Brocess is not null) {
			if (OperatingSystem.IsWindows()) {
				if (Brocess.MainWindowHandle is nint handle && U32.IsWindow(handle)) {
					_ = U32til.BringWindowToForeground(handle);
				}
			} else if (OperatingSystem.IsMacOS()) {
				if (MacOSUtil.SetForegroundWindow(Brocess.Id)) {
					Brocess.Refresh();
				}
			}
		}

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
			Toaster.Info($"Requesting timezone/geo data for {Settings.Profile.Proxy.WebProxy?.Address?.Host ?? "local"}");
			var ipapi = await GeoIpApi.GetIpapi(Settings.Profile.Proxy.WebProxy, e => Toaster.Error(e)) ?? new() {
				timezone = "Pacific/Honolulu",
				lat = 34.052235,
				lon = -118.243683,
				tzSystem = true
			};
			Toaster.Info($"Timezone: {ipapi.timezone}, Lat: {ipapi.lat}, Lon: {ipapi.lon}");

			// set the extension settings
			AddonsServer.Instance.AddonInstances[SessionId] = new {
				proxy = new {
					enabled = Settings.Profile.Proxy.CanUse,
					type = "http",
					server = Settings.Profile.Proxy.Server,
					host = Settings.Profile.Proxy.Host,
					port = Settings.Profile.Proxy.Port,
					username = Settings.Profile.Proxy.UserName,
					password = Settings.Profile.Proxy.Password,
				},
				urls = new {
					start = Settings.Profile.StartUrl,
					homePages = Settings.Profile.DefaultHomePageSettings,
				},
				tz = new {
					enabled = Settings.Profile.Emulations.AutoTimezone,
					zone = ipapi.timezone,
					system = ipapi.tzSystem,
					locale = "en-" + ipapi.countryCode,
				},
				geo = new {
					enabled = Settings.Profile.Emulations.SpoofGeoLocation,
					ipapi.lat,
					ipapi.lon,
				},
				canvas = new {
					enabled = Settings.Profile.Emulations.SpoofCanvasFingerprint,
				},
				webgl = new {
					enabled = Settings.Profile.Emulations.SpoofWebGLFingerprint,
				},
				rects = new {
					enabled = Settings.Profile.Emulations.SpoofClientRects,
				},
				fonts = new {
					enabled = Settings.Profile.Emulations.SpoofFontFingerprint,
				},
				audio = new {
					enabled = Settings.Profile.Emulations.SpoofAudio,
				},
				navi = new {
					enabled = Settings.Profile.Emulations.SpoofNavigator,
				},
			};
			await InitializeExtensionPath();
			if (LoadedTCS.Task.IsCompleted)
				return;

			// StartProcess
			Brocess = new Process {
				StartInfo = new() {
					FileName = ExePath,
					Arguments = GetCommandLineArguments(),
					UseShellExecute = false,
					CreateNoWindow = true,
				},
				EnableRaisingEvents = true,
			};
			Brocess.Start();

			await Task.Delay(1800);
			await WaitForWinHandle();

			if (!Brocess.HasExited)
				_ = LoadedTCS.TrySetResult(true);
			else
				Close();
		}
	}

	public virtual Task Ensure() => Task.CompletedTask;
	public virtual string ExeDir => Path.GetDirectoryName(ExePath) ?? string.Empty;
	public abstract string PrefsFile { get; }
	public abstract string ExePath { get; }
	protected abstract Task InitializeExtensionPath();
	protected abstract string GetCommandLineArguments();

	protected virtual async Task WaitForWinHandle() {
		// if (OperatingSystem.IsMacOS()) {
		Brocess!.Exited += (s, e) => { Close(); };
		
		if (await TaskUtil.AwaitFor(
				() => Brocess?.HasExited == false && MacOSUtil.FindWindowByPID(Brocess.Id) != null,
				36,
				1000
			)) {
			MacOSWindowListener.Instance.AddPid(Brocess!.Id);
		}
	}
}
