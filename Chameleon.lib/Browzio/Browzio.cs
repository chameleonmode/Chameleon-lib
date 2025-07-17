using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using chameleon.assets;
using Chameleon.lib.Browzio.Browsers;
using Chameleon.lib.Browzio.Services;
using Chameleon.lib.Services;
using Chameleon.lib.Util;

namespace Chameleon.lib.Browzio;

#region types
public enum BrowserType { Chrome, Firefox, Brave }

public interface IBrowserInstance {
  public enum Event { Unknown, Error, Closed, Opened, Foreground, Background }
  public record EventArgs(BrowserSetting Settings, Event Event);
  Process? Brocess { get; set; }
  BrowserSetting Settings { get; init; }
  string SessionId { get; }
  void InvokeEvent(Event @event);
  void Close();
  Task Closee();
  Task Ensure();
  Process Brocessor(string url);
  TaskCompletionSource<bool> LoadedTCS { get; }
  Task Initialize(object? param = null);
  event Action<object, EventArgs>? OnEvent;
}
public record BrowserOption(BrowserType Option) {
  public string IconName { get; } = Option.ToString().ToLower();
}
public class BrowserProxy {
  public string? HostForRequest => Host?.Contains("proxy.chameleonmode.com") == true ?
    "proxy.packetstream.io"
    : Host;
  public string? Server => CanUse ? $"{Host}:{Port}" : null;
  public string? ServerForRequest => CanUse ? $"http://{Server}" : null;
  public WebProxy? WebProxy => CanUse ? new WebProxy(Server) {
    Credentials = new NetworkCredential(UserName, Password)
  } : null;

  public bool CanUse => Host.IsNot() && Port > 0;
  public bool HasLogin => UserName.IsNot() && Password.IsNot();

  private string? _host;
  private int _port = 80;
  private string? _userName;
  private string? _password;

  public string? Host {
    get => _host;
    set => _host = value?.Trim();
  }

  public string? UserName {
    get => _userName;
    set => _userName = value?.Trim();
  }

  public string? Password {
    get => _password;
    set => _password = value?.Trim();
  }

  public int Port {
    get => _port;
    set {
      if (value is < 0 or > 65535) value = 0;
      _port = value;
    }
  }
}
public class BrowserProfile {
  public int Id { get; init; } = -1; // -1 is a special value for the default profile
  public bool Extensions { get; init; } = true;
  public BrowserProxy Proxy { get; set; } = new();
  public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();

  public string[] Bookmarks { get; init; } = IoC.GetJsonValue<string[]>(nameof(Bookmarks)) ?? [];

  public string StartPage { get; set; } = IoC.GetValue(nameof(StartPage))
    .Let(url =>
      url.Is()
        ? "about:blank"
        : Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
          (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
            ? uriResult.AbsoluteUri
            : "http://" + url
    );
}
public record BrowserSetting(BrowserType BrowserType, BrowserProfile Profile) {
  public int Port { get; set; } = 0;
  public string CachePath => FilePaths.EnsureDirectoryExists(
    FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()
  );
  public string ExtensionsPath =>
    Path.Combine(FilePaths.AppTempDir, "Chromo", BrowserType.ToString(), Profile.Id.ToString());

  private IBrowserInstance? browser;
  public IBrowserInstance Browser => browser ??= BrowserType switch {
    BrowserType.Brave => new Brave() { Settings = this },
    BrowserType.Chrome => new Chrome() { Settings = this },
    BrowserType.Firefox => new Firefox() { Settings = this },
    _ => throw new NotImplementedException(),
  };
}
public class EmulationOptions {
  public bool AutoTimezone { get; set; } = true;
  public bool SpoofGeoLocation { get; set; } = true;
  public bool SpoofWebGLFingerprint { get; set; } = true;
  public bool SpoofCanvasFingerprint { get; set; } = true;
  public bool SpoofClientRects { get; set; } = true;
  public bool SpoofFontFingerprint { get; set; } = true;
  public bool SpoofAudio { get; set; } = true;
  public bool DisableWebRTC { get; set; } = true;
  public bool SpoofNavigator { get; set; } = false;
}
#endregion

public static class BrowserInfo {
  public record class Info(string Name, string Path) {
    public override string ToString() {
      return Name ?? Path;
    }
    public bool Exists => !string.IsNullOrEmpty(Path) && File.Exists(Path);
  }

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
      using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(Path.Combine(registryKey, executable));
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
      using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(uninstallKey);
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

  static Info FindByName(string executable) {
    if (OperatingSystem.IsMacOS()) {
      var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
      var bravePath = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
      var firefoxPath = "/Applications/firefox.app/Contents/MacOS/firefox";

      return executable switch {
        "chrome.exe" => File.Exists(chromePath) ? new Info("chrome", chromePath) : null,
        "brave.exe" => File.Exists(bravePath) ? new Info("brave", bravePath) : null,
        "firefox.exe" => File.Exists(firefoxPath) ? new Info("brave", firefoxPath) : null,
        _ => null
      } ?? throw new NotSupportedException(
            $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
    } else if (OperatingSystem.IsWindows()) {
      var (installed, filepath) = CheckApplication(executable);
      if (installed && !string.IsNullOrWhiteSpace(filepath)) return new Info(executable, filepath);
    }

    throw new NotSupportedException(
          $"{char.ToUpper(executable[0]) + executable[1..]} browser is not installed.");
  }

  public static Info Find(BrowserType BrowserType) => BrowserType switch {
    BrowserType.Chrome => FindByName("chrome.exe"),
    BrowserType.Brave => FindByName("brave.exe"),
    BrowserType.Firefox => FindByName("firefox.exe"),
    _ => throw new NotSupportedException("Browser type not found."),
  };
}

public class Browzio : IStartUp {
  public static class State {
    public static bool Staging { get; } = Debugger.IsAttached || Environment.GetEnvironmentVariable("CHAMELEON_DEV_MODE") == "true";
  }
  public static class Extensions {
    public static string? Version { get => IoC.GetValue(nameof(Extensions)); set => IoC.SetValue(nameof(Extensions), value!); }
    public static string AddonDevPath => OperatingSystem.IsMacOS()
      ? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons"
      : @"C:\repos\Chameleon-lib\Chameleon.Assets\addons";

    public static string Chromium => FilePaths.EnsureDirectoryExists(
      FilePaths.AppDataDir, "extensions", "chromium"
    );
    public static string Chromeleon => Path.Combine(
      State.Staging && Directory.Exists(AddonDevPath)
      ? AddonDevPath
      : Chromium, "chromeleon");

    public static string Gecko => Resources.Assert(
      FilePaths.AppDataDir, "extensions", "gecko"
    );
    public static string Geckoleon => Path.Combine(
      State.Staging && Directory.Exists(AddonDevPath)
      ? AddonDevPath
      : Gecko, "geckoleon.xpi");
  }
  public static class Factory {
    public static BrowserSetting Chrome(BrowserProfile profile) {
      return new BrowserSetting(BrowserType.Chrome, profile) {
        Port = Processez.NextFreePort(9613)
      };
    }
    public static BrowserSetting Chrome(string url) {
      return Chrome(new BrowserProfile {
        StartPage = url,
        Extensions = false,
        Emulations = new(),
      });
    }

    public static BrowserSetting Brave(BrowserProfile profile) {
      return new BrowserSetting(BrowserType.Brave, profile);
    }

    public static BrowserSetting Firefox(BrowserProfile profile) {
      return new BrowserSetting(BrowserType.Firefox, profile);
    }
  }

  public TaskCompletionSource<bool> Initialized { get; } = new();

  public async Task Init() {
    await AddonsServer.I.Initialized.Task;
    if (Extensions.Version != IoC.Assembled) {
      Extensions.Version = IoC.Assembled;
      await Resources.CopyFile("addons", "geckoleon.xpi", Extensions.Gecko);
      await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);
    }

    _ = Initialized.TrySetResult(true);
  }

  Browzio() { }
  public static Browzio I { get; } = new();
}