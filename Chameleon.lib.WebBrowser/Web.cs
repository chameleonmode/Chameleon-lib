using chameleon.assets;
using Chameleon.lib.Const;
using Chameleon.lib.WebBrowser.Services;

namespace Chameleon.lib.WebBrowser;

public static class Project {
  public static class Extensions {
    public static string Chromium => Resources.Assert(
      FilePaths.AppDataDir, "chromium", "extensions"
    );
  }
  
  public static TaskCompletionSource<bool> Initialized { get; } = new();
  public static async Task<bool> Init() {
    await AddonsServer.Instance.Start();
    
    Directory.Delete(Extensions.Chromium, true);
    _ = await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);
    return Initialized.TrySetResult(true);
  }
}

