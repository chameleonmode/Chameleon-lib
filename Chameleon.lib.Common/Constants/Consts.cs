using Chameleon.lib.Common.Util;
using Chameleon.lib.Const;

namespace Chameleon.lib.Common.Constants;
public static class Consts {
	public const string AppSettingsFileName = "appsettings.json";
	public const string LocalHostUrl = "http://localhost:21021/api";
	public const string ApiBaseUrl = "https://api.chameleonmode.com/api";
	public const string NotionProfile = "https://www.notion.so/4-Setting-Up-Your-First-Profile-d2d001b2127e4a0e8e083fc13ad4cf99";
	public const string NotionUrl = "https://intercom.help/chameleonmode/en";
	public const string ApiSocialAnimalUrl = "https://api.socialanimal.com/api/v1/search";
	public const string WebsiteUrl = "https://chameleonmode.com/";
	public const string SupportUrl = "https://intercom.help/chameleonmode/en";
	public const string FacebookGroupUrl = "https://www.facebook.com/groups/962349154557466";
	public const string PricingUrl = "https://chameleonmode.com/pricing/";
	public const string DefaultHomePage = "https://example.com/";
	public const int PageinationPageItems = 13;

	public static string AppDataDir => IOtil.EnsureDirectoryExists(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Variables.AppName));
	public static string AppDataLocalDir => IOtil.EnsureDirectoryExists(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Variables.AppName));

	public static class Api {
		public const string ApiBaseUrl = "https://api.chameleonmode.com/api/";
		public const string ServicesPath = "services/app/";
		public static class Endpoints {
			public const string Profile					= $"{ServicesPath}profile/";
			public const string Folder					= $"{ServicesPath}folder/";
			public const string Person					= $"{ServicesPath}person/";
			public const string Business				= $"{ServicesPath}business/";
			public const string Address					= $"{ServicesPath}address/";
			public const string Credentia				= $"{ServicesPath}credential/";
			public const string Country					= $"{ServicesPath}country/";
			public const string Proxy						= $"{ServicesPath}proxy/";
			public const string ProxyCredit			= $"{ServicesPath}proxycredit/";
			public const string AssistantUser		= $"{ServicesPath}assistantuser/";
			public const string ShareFolders		= $"{ServicesPath}sharefolders/";
			public const string BrowserSettings = $"{ServicesPath}userdefaultsettings/";
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
		public const string JsonFontsEmbeddedDir = "embedded://chameleon.assets.json.fa_symbolfonts.json";
	}

	public static class Addons {
		public const string AddonsEmbeddedDir = "embedded://chameleon.assets/addons";
		public static string AddonExtentionDir => Path.Combine(FilePaths.AppTempDir, "Addons");
		public static string CachedExtentionDir => Path.Combine(FilePaths.AppTempDir, "eleonextcache");
		public static string DefaultExtensionsFolderPath => OperatingSystem.IsMacOS()
			? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../Resources/BrowserExtensions")
			: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserExtensions");

		public static string DefaultExtensionsFolderPath_Brave => Path.Combine(DefaultExtensionsFolderPath, "brave");
		public static string DefaultExtensionsFolderPath_Chrome => Path.Combine(DefaultExtensionsFolderPath, "chrome");
		public static string DefaultExtensionsFolderPath_FF => Path.Combine(DefaultExtensionsFolderPath, "firefox");
	}

	public static class Browser {
		public const string Foxameleon = "Foxameleon";
		public const string CachedFoxameleon = "CachedFoxameleon";

		public static string LocalFirefoxDirPath => OperatingSystem.IsMacOS()
			? Path.Combine(AppDataLocalDir, Foxameleon, "firefox.app")
			: Path.Combine(AppDataLocalDir, Foxameleon);
		public static string LocalFirefoxExePath => OperatingSystem.IsMacOS()
			? Path.Combine(LocalFirefoxDirPath, "Contents", "MacOS", "firefox")
			: Path.Combine(LocalFirefoxDirPath, "firefox.exe");
	}

	public static class Permissions {
		public const string Pages = "Pages.";

		public const string Pages_Outreach = Pages + "Outreach";
		public const string Pages_Prospector = Pages + "Prospector";
		public const string Pages_YouTube = Pages + "YouTube";
		public const string Pages_YouTube_Config = Pages_YouTube + ".Config";
		public const string Pages_RSS = Pages + "RSS";
		public const string Pages_Curate = Pages + "Curate";
		public const string Pages_Curate_Config = Pages_Curate + ".Config";
		public const string Pages_CreateProfiles = Pages + "CreateProfiles";
		public const string Pages_DeleteProfiles = Pages + "DeleteProfiles";
		public const string Pages_Proxy = Pages + "Proxy";
		public const string Pages_Proxy_Config = Pages_Proxy + ".Config";
		public const string Pages_ProxyCredit = Pages + "ProxyCredit";
		public const string Pages_ImportExport = Pages + "ImportExport";
		public const string Pages_AssistantUsers = Pages + "AssistantUsers";

		public const string Pages_Tenants = "Pages.Tenants";

		public const string Pages_Users = "Pages.Users";
		public const string Pages_Users_Activation = "Pages.Users.Activation";

		public const string Pages_Users_Primary = Pages_Users + ".Primary";
		public const string Pages_Users_Assistant = Pages_Users + ".Assistant";

		public const string Pages_Roles = "Pages.Roles";

		public const string Pages_Licences = "Pages.Licences";

		public const string Pages_ProxyCreditPlans = "Pages.ProxyCreditPlans";

		public const string Pages_ProxyCredits = "Pages.ProxyCredits";
		public const string Pages_ProxyCredits_Create = Pages_ProxyCredits + ".Create";
		public const string Pages_ProxyCredits_Update = Pages_ProxyCredits + ".Update";

		public const string Automation = "Automaation";
		public const string Automation_Edit = Automation + ".Edit";
	}
}
