using System.Diagnostics;
using System.Net;
using System.Reflection;
using chameleon.assets;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Util;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser;

#region types
public interface IBrowserInstance : IAmInitializer {
  event Delegatorz.Event<SysBrowserEvent>? OnEvent;
  Process? Brocess { get; set; }
  SysBrowserSettings Settings { get; init; }
  string SessionId { get; }
  void InvokeEvent(Enums.SysBrowserEventType eventType);
  void Close();
  Task Ensure();
}
#endregion

#region models
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
  public int Id { get; set; }
  public BrowserProxy Proxy { get; set; } = new();
  public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();

  public string[] DefaultHomePageSettings { get; init; } =
    IoC.GetJsonValue<string[]>(nameof(DefaultHomePageSettings))
      .Let(urls => urls != null && urls.Length > 0 ? new[] { urls[new Random().Next(urls.Length)] } : ["example.com"]);

  public string StartUrl { get; init; } =
    IoC.GetJsonValue<string[]>(nameof(DefaultHomePageSettings))
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
}
public record SysBrowserEvent(SysBrowserOpenOptions OpenOptions, SysBrowserEventType EventType);
public record SysBrowserOpenOptions(SystemBrowserType BrowserType, BrowserProfile Profile);
public record SysBrowserSettings(SysBrowserOpenOptions OpenOptions, int Port) {
  public SystemBrowserType BrowserType => OpenOptions.BrowserType;
  public BrowserProfile Profile => OpenOptions.Profile;

  public string SysBrowserProfileCachePath => IOtil.EnsureDirectoryExists(
    Path.Combine(FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString())
    );

  private string? destextPath;
  public string DestExtentionsDir {
    get {
      if (destextPath == null) {
        destextPath = Path.Combine(FilePaths.AppTempDir, "Addons", BrowserType.ToString(), Profile.Id.ToString());
        IOtil.DeleteDir(destextPath);
        destextPath = IOtil.EnsureDirectoryExists(Path.Combine(destextPath, Guid.NewGuid().ToString()));
      }
      return destextPath;
    }
  }
  private string? cachedExtentionsDir;
  public string CachedExtentionsDir {
    get {
      cachedExtentionsDir ??= IOtil.EnsureDirectoryExists(
        Path.Combine(FilePaths.AppDataDir, "cache", BrowserType.ToString(), Profile.Id.ToString())
      );
      return cachedExtentionsDir;
    }
  }

  public SysBrowserEvent CreateEvent(SysBrowserEventType sysBrowserEventType) => new(OpenOptions, sysBrowserEventType);

  public Dictionary<ExtensionType, (string? settings, string guid, string destDir)> ExtentionsDirs { get; } = [];
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
  public static class Extensions {
    public static string Chromium => Resources.Assert(
      FilePaths.AppDataDir, "extensions", "chromium"
    );
    public static string Gecko => Resources.Assert(
      FilePaths.AppDataDir, "extensions", "gecko"
    );
    public static string Geckoleon => Resources.Assert(
      Gecko, "geckoleon.xpi"
    );
  }

  public static TaskCompletionSource<bool> Initialized { get; } = new();
  public static async Task<bool> Init() {
    await AddonsServer.Instance.Start();

    var version = IoC.GetValue(nameof(Extensions));
    if (version is not string ver || ver != lib.Project.Assembled) {
      IoC.Instance.Config?.SetValue(nameof(Extensions), lib.Project.Assembled);
      await Resources.CopyFile("addons", "geckoleon.xpi", Extensions.Gecko);
      await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);
    }
    
    return Initialized.TrySetResult(true);
  }
}

