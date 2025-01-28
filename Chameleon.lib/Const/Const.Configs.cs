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
					"http://127.0.0.1:3042"
#else
		      "https://chameleon-abs.onrender.com"
#endif
		;
	}

	public static class Endpoints {
		public static class DB {
			public const string USER = "/db/user";
			public const string COOKIES = "/db/cookies";
		}
		
		public const string Users = "/users";
		public const string DataInteractions = "/dataInteractions";
	}
}
