using Chameleon.lib.Abs;

namespace Chameleon.lib.Const;
public static class Configs {
	public static class Oidc {
		public const string Domain = "dev-gcjhdlkot8s8v2vr.us.auth0.com";
		public const string ClientId = "dEtvplqXMKlDV1xSuuPfTLoWxtR8uMJv";
		public const string ApiAudience = "https://api.chameleonmode.com/";
		public const string Auth0Audience = "https://dev-gcjhdlkot8s8v2vr.us.auth0.com/userinfo";
	}
	public static class Urls {
		public static string ABS_BASE_URL =>
#if DEBUG
						"http://localhost:3001"
#else
            "https://abswebapp.azurewebsites.net"
#endif
		;
		public static string ABS_PLATFORMATIC_BASE_URL =>
#if DEBUG
					"http://127.0.0.1:3000"
#else
        "https://abswebapp.azurewebsites.net"
#endif
	;
	}

	public static class Endpoints {
		public const string Users = "/users";
		public const string Cookies = "/cookies";
	}

	public static class IoCKeys {
		public const string IAuth = $"{nameof(ABService)}{nameof(IAuth)}v3";
		public const string ITennant = $"{nameof(ABService)}{nameof(ITennant)}";
	}
}
