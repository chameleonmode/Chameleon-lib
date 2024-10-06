using Chameleon.lib.Common.Util;

namespace Chameleon.lib.Common.Constants;
public static class Consts {
	public const string AppName = "Chameleon";
	public const string AppSettingsFileName = "appsettings.json";

	public static string AppTempDir {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Path.GetTempPath(), AppName));
		}
	}
	public static string AppDataDir {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName));
		}
	}
	public static string AppDataLocalDir {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName));
		}
	}

	public static class Http {
		public const string ChameleonModeHost = "proxy.chameleonmode.com";
		public const string PacketStreamHost = "proxy.packetstream.io";
		public const string HttpScheme = "http://";
		public const string HttpsScheme = "https://";
		public const string UrlSchemeEnd = "://";
		public const string DomainLevelDelimiter = ".";
	}
	public static class Json {
		public const string JsonFontsEmbeddedDir = "embedded://chameleon.assets/json/fa_symbolfonts.json";
	}

	public static class Addons {
		public const string AddonsEmbeddedDir = "embedded://chameleon.assets/addons";
		public static string AddonExtentionDir => Path.Combine(AppTempDir, "Addons");
		public static string DefaultExtensionsFolderPath => OperatingSystem.IsMacOS()
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../Resources/BrowserExtensions")
			: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserExtensions");

		public static string DefaultExtensionsFolderPath_Brave => Path.Combine(DefaultExtensionsFolderPath, "brave");
		public static string DefaultExtensionsFolderPath_Chrome => Path.Combine(DefaultExtensionsFolderPath, "chrome");
		public static string DefaultExtensionsFolderPath_FF => Path.Combine(DefaultExtensionsFolderPath, "firefox");
	}

	public static class Browser {
		public const string Foxameleon = "Foxameleon";

		public static string LocalFirefoxDirPath => OperatingSystem.IsMacOS()
			? Path.Combine(AppDataLocalDir, Foxameleon, "firefox.app")
			: Path.Combine(AppDataLocalDir, Foxameleon);
		public static string LocalFirefoxExePath => OperatingSystem.IsMacOS()
			? Path.Combine(LocalFirefoxDirPath, "Contents", "MacOS", "firefox")
			: Path.Combine(LocalFirefoxDirPath, "firefox.exe");
	}
}
