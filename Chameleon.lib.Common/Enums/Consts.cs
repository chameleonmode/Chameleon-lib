using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Chameleon.lib.Common.Util;

namespace Chameleon.lib.Common.Enums;
public static partial class Consts {
	public const string AppName = "Chameleon";

	public static string AppTempDir {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Path.GetTempPath(), AppName));
		}
	}
	public static string AppDataRoamingDir {
		get {
			return IOtil.EnsureDirectoryExists(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName));
		}
	}
	public static string AppDataDir {
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

	public static partial class Regexers {
		[GeneratedRegex(@"user_pref\(""(.*?)"", (\""(.*?)\""|.*?)\);")]
		public static partial Regex UserPrefRegex();
	}

	public static class Addons {
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
			? Path.Combine(AppDataDir, Foxameleon, "firefox.app")
			: Path.Combine(AppDataDir, Foxameleon);
		public static string LocalFirefoxExePath => OperatingSystem.IsMacOS()
			? Path.Combine(LocalFirefoxDirPath, "Contents", "MacOS", "firefox")
			: Path.Combine(LocalFirefoxDirPath, "firefox.exe");
	}
}
