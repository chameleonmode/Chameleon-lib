using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using chameleon.assets;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Browsers;
using Chameleon.lib.WebBrowser.Services;
using Chameleon.lib.WebBrowser.System.Brave;
using Chameleon.lib.WebBrowser.System.Chrome;
using Chameleon.lib.WebBrowser.System.Firefox;

namespace Chameleon.lib.WebBrowser;

	public enum BrowserType {
		Unknown,
		Chromium,
		[Description("chrome")] Chrome,
		[Description("firefox")] Firefox,
		[Description("brave")] Brave,
	}

#region models
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
  public int Port { get; set; } = 0;
  public BrowserProxy Proxy { get; set; } = new();
  public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();

  public string[] Bookmarks { get; init; } =
    IoC.GetJsonValue<string[]>(nameof(Bookmarks))
      .Let(urls => urls != null && urls.Length > 0 ? new[] { urls[new Random().Next(urls.Length)] } : ["example.com"]);

  public string StartUrl { get; init; } =
    IoC.GetJsonValue<string[]>(nameof(Bookmarks))
      .Let(urls => urls != null && urls.Length > 0 ? urls[new Random().Next(urls.Length)] : "example.com")
      .Let(randomUrl => Uri.TryCreate(randomUrl, UriKind.Absolute, out var uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
          ? uriResult.AbsoluteUri
          : "http://" + randomUrl);
}
public record class BrowserRecord(string Name, string Path) {
  public override string ToString() {
    return Name ?? Path;
  }
  public bool Exists => !string.IsNullOrEmpty(Path) && File.Exists(Path);
}
public static class FactorySettings {
  public static BrowserSetting Chrome(BrowserProfile profile) {
    return new BrowserSetting(BrowserType.Chrome, profile);
  }
  public static BrowserSetting Chrome(string url) {
    return Chrome(new BrowserProfile {
      StartUrl = url,
      Extensions = false,
      Emulations = new(),
      Port = TcpUtil.NextFreePort(9613)
    });
  }

  public static BrowserSetting Brave(BrowserProfile profile) {
    return new BrowserSetting(BrowserType.Brave, profile);
  }

  public static BrowserSetting Firefox(BrowserProfile profile) {
    return new BrowserSetting(BrowserType.Firefox, profile);
  }
}
public record BrowserSetting(BrowserType BrowserType, BrowserProfile Profile) {
  public string BrowserCache => Resources.Assert(FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString());
  public string Cached => Resources.Assert(FilePaths.AppDataDir, "cache", BrowserType.ToString(), Profile.Id.ToString());

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

public static class Project {
  public static bool Staging { get; } = true && (Debugger.IsAttached || Environment.GetEnvironmentVariable("CHAMELEON_DEV_MODE") == "true");
  
  public static class Extensions {
    public static string Chromium => Resources.Assert(
      FilePaths.AppDataDir, "extensions", "chromium"
    );
    
    public static string Chromeleon { 
      get {
        var devPath = Path.Combine(GetDevChromePath(), "chromeleon");
        var prodPath = Path.Combine(Chromium, ExtensionType.chromeleon.ToString());
        
        if (Staging && Directory.Exists(devPath)) {
          return devPath;
        } else {
          return prodPath;
        }
      }
    }

    public static string Gecko => Resources.Assert(
      FilePaths.AppDataDir, "extensions", "gecko"
    );
    public static string Geckoleon => Path.Combine(Gecko, "geckoleon.xpi");

	  public static string Defaults => OperatingSystem.IsMacOS()
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..Resources/browser/extensions")
			: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\browser\\extensions");
			
    private static string GetDevChromePath() {
      if (OperatingSystem.IsMacOS()) {
        return Path.Combine("/Users/dev/src/chameleon-playwright/dist");
      } else {
        // Windows equivalent just as placeholder
        return Path.Combine(@"C:\Projects\Chameleon\chameleon-playwright\dist");
      }
    }
  }

  public static TaskCompletionSource<bool> Initialized { get; } = new();
  public static async Task<bool> Init() {
    await AddonsServer.Instance.Start();

    if (IoC.GetValue(nameof(Extensions)) is not string ver || ver != IoC.Assembled) {
      IoC.SetValue(nameof(Extensions), IoC.Assembled);
      await Resources.CopyFile("addons", "geckoleon.xpi", Extensions.Gecko);
      await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);
    }

    return Initialized.TrySetResult(true);
  }
}

