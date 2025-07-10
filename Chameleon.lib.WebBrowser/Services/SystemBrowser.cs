using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Helpers;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Browsers;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;
using Microsoft.Win32;

namespace Chameleon.lib.WebBrowser.Services;
public static class BrowserInfo {

   [SupportedOSPlatform("windows")]
   private static (bool Installed, string FilePath) CheckApplication(string executable) {
      // Check common installation paths
      string[] commonPaths = [
         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), executable),
         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), executable)
      ];

      foreach (var path in commonPaths) {
         if (File.Exists(path)) return (true, path);
      }

      // Check registry
      string[] registryKeys = [
         @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
         @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
      ];

      foreach (var registryKey in registryKeys) {
         using var key = Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executable));
         if (key != null) {
            var path = key.GetValue(null) as string;
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) return (true, path);
         }
      }

      // Check for user-specific installation
      var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      var userSpecificPaths = Directory.GetFiles(appDataPath, executable, SearchOption.AllDirectories);
      if (userSpecificPaths.Length != 0) return (true, userSpecificPaths.First());

      // Check uninstall registry keys
      string[] uninstallKeys = [
         @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
         @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
      ];

      foreach (var uninstallKey in uninstallKeys) {
         using var key = Registry.LocalMachine.OpenSubKey(uninstallKey);
         if (key != null) {
            foreach (var subKeyName in key.GetSubKeyNames()) {
               using var subKey = key.OpenSubKey(subKeyName);
               var displayName = subKey?.GetValue("DisplayName") as string;
               if (
                  !string.IsNullOrEmpty(displayName) &&
                  displayName.Contains(Path.GetFileNameWithoutExtension(executable), StringComparison.OrdinalIgnoreCase)
               ) {
                  var installLocation = subKey?.GetValue("InstallLocation") as string;
                  if (!string.IsNullOrEmpty(installLocation)) {
                     var fullPath = Path.Combine(installLocation, executable);
                     if (File.Exists(fullPath)) return (true, fullPath);
                  }
               }
            }
         }
      }

      return (false, string.Empty);
   }

   static BrowserRecord FindByName(string executable) {
      if (OperatingSystem.IsMacOS()) {
         var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
         var bravePath = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
         var firefoxPath = "/Applications/firefox.app/Contents/MacOS/firefox";

         return executable switch {
            "chrome.exe" => File.Exists(chromePath) ? new BrowserRecord("chrome", chromePath) : null,
            "brave.exe" => File.Exists(bravePath) ? new BrowserRecord("brave", bravePath) : null,
            "firefox.exe" => File.Exists(firefoxPath) ? new BrowserRecord("brave", firefoxPath) : null,
            _ => null
         } ?? throw new NotSupportedException(
               $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
      } else if (OperatingSystem.IsWindows()) {
         var (installed, filepath) = CheckApplication(executable);
         if (installed && !string.IsNullOrWhiteSpace(filepath)) return new BrowserRecord(executable, filepath);
      }

      throw new NotSupportedException(
            $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
   }

   public static BrowserRecord Find(BrowserType BrowserType) => BrowserType switch {
      BrowserType.Chrome => FindByName("chrome.exe"),
      BrowserType.Brave => FindByName("brave.exe"),
      BrowserType.Firefox => FindByName("firefox.exe"),
      _ => throw new NotSupportedException("Browser type not found."),
   };
}

public class SystemBrowser {
	private readonly WindowEventHandler? windowEventHandler;
	public int TimeOut { get; } = 14;
	public ConcurrentDictionary<LaunchOptions, IBrowserInstance> Instances { get; } = [];
	public ConcurrentDictionary<int, List<Delegatorz.Event<BrowserEvent>>> Observers { get; } = [];
	SystemBrowser() {
		if (OperatingSystem.IsWindows()) {
			windowEventHandler = new WindowEventHandler();
			windowEventHandler.OnDestroy += async (obj) => {
				await EX.Try(async () => {
					for (var i = Instances.Count - 1; i >= 0; i--) {
						if (
							Instances.TryGetValue(Instances.Keys.ElementAt(i), out var browser) &&
							browser != null &&
						 	await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(3)) == false
						) {
							if(browser.Brocess?.HasExited == false || browser.Brocess?.MainWindowHandle == IntPtr.Zero) await browser.Closee();
							browser.Close();
						}
					}
				});
			};
		}
	}

	public async Task<IBrowserInstance> Open(BrowserSettings settings) {
		if (settings.Profile.Extensions) _ = await Project.Initialized.Task;
		// TODO: test node console standard server launcher vs tcp server 
		// await NodeServerLauncher.Instance.StartServer();
		// TODO: move to app startup or possibly add a lib startup module
		// await AddonsServer.Instance.Start();
		settings.Profile.Port = settings.Profile.Port == 0 ? TcpUtil.NextFreePort(9613) : settings.Profile.Port;
		IBrowserInstance browser = settings.BrowserType switch {
			BrowserType.Brave => new Brave() { Settings = settings },
			BrowserType.Chrome => new Chrome() { Settings = settings },
			BrowserType.Firefox => new Firefox() { Settings = settings },
			_ => throw new NotImplementedException(),
		};
		if (
			!Instances.ContainsKey(settings.OpenOptions) &&
			!Instances.TryAdd(settings.OpenOptions, browser) &&
			!settings.Profile.Extensions
		) throw new Exception("Browser linkage failed. Try closing all running profiles and restarting the application.");

		browser.OnEvent += async (sender, args) => {
			if (args.EventType == BrowserEventType.Closed) {
				_ = await browser.LoadedTCS.Task;
				_ = Instances.TryRemove(settings.OpenOptions, out _);
			}
			//if(Observers.TryGetValue(settings.Profile.Id, out var value)) value.ForEach(x => x.Invoke(sender, args));
			if (Observers.TryGetValue(settings.Profile.Id, out var value)) value.ForEach(x => x.Invoke(sender, args));
		};
		_ = browser.Initialize();
		if (await browser.LoadedTCS.Task.WaitAsync(TimeSpan.FromSeconds(settings.Profile.Extensions ? TimeOut : 6))) browser.InvokeEvent(BrowserEventType.Opened);
		else if (!settings.Profile.Extensions) throw new Exception("Browser needs to be restarted to apply changes. Please close and reopen your browser.");
		Instances[settings.OpenOptions] = browser;
		return browser;
	}
	public async Task<IBrowserInstance?> Open(LaunchOptions options) {
		var browser = Instances.FirstOrDefault(x => x.Key.Profile.Id == options.Profile.Id && x.Key.BrowserType == options.BrowserType).Value;
		if (browser == null) {
			return await EX.Catch(async () => browser = await Open(new BrowserSettings(options)), e => {
				Toaster.Error(e.Message);
				if (browser != null) browser.InvokeEvent(BrowserEventType.Closed);
				else if (Observers.TryGetValue(options.Profile.Id, out var events)) events.ForEach(x => x.Invoke(this, new(options, BrowserEventType.Closed)));

				if (e is InvalidDataException or TimeoutException && Instances.ContainsKey(options)) _ = Instances.TryRemove(options, out _);
				_ = browser?.LoadedTCS.TrySetResult(false);
			});
		} else if (browser.Brocess is null || browser.Brocess.HasExited) {
			await browser.Closee();
			browser.Close();
			await Task.Delay(256);
			return await Open(options);
		}
		return browser;
	}

	public IEnumerable<BrowserType> HasInstanceOf(int id, Delegatorz.Event<BrowserEvent> action) {
		if (Observers.TryGetValue(id, out var value)) value.Add(action);
		else Observers[id] = [action];

		return Instances
			.Where(x => x.Value?.Settings.Profile.Id == id)
			.Select(b => b.Value?.Settings.BrowserType ?? BrowserType.Unknown);
	}

	// Singleton
	public static SystemBrowser I { get; } = new();
}
