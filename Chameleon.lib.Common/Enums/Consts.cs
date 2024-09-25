using System.Security.Cryptography;

using Chameleon.lib.Common.Util;

namespace Chameleon.lib.Common.Enums;
public static class Consts {
	public const string AppName = "Chameleon";

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

	public static class Http {
		public const string ChameleonModeHost = "proxy.chameleonmode.com";
		public const string PacketStreamHost = "proxy.packetstream.io";
		public const string HttpScheme = "http://";
		public const string HttpsScheme = "https://";
		public const string UrlSchemeEnd = "://";
		public const string DomainLevelDelimiter = ".";
	}

	public static class Addons {
		public const string HttpScheme = "http://";
		public const string HttpsScheme = "https://";
		public const string UrlSchemeEnd = "://";
		public const string DomainLevelDelimiter = ".";

		public static string AddonExtentionDir => Path.Combine(AppTempDir, "Addons");
		public static string DefaultExtensionsFolderPath => OperatingSystem.IsMacOS()
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../Resources/BrowserExtensions")
			: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserExtensions");
	}
}
