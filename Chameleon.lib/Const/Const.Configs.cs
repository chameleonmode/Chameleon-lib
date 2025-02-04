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
					//"http://127.0.0.1:3042"
					//"https://chameleon-ws.onrender.com"
					"https://chameleon-ws-pr-1.onrender.com"
#else
					"https://chameleon-ws.onrender.com"
#endif
		;
	}

	public static class Endpoints {
		public static class APP {
			const string BASE = "/app";
			public const string LATEST = $"{BASE}/latest";
			public const string DOWNLOAD = $"{BASE}/download";
		}
		public static class DB {
			const string BASE = "/db";
			public const string USER = $"{BASE}/user";
			public const string COOKIES = "/db/cookies";
		}

		public static class LICENSE {
			const string BASE = "/license";
			public const string ACTIVATE = $"{BASE}/activate";
		}

		public const string Users = "/users";
		public const string DataInteractions = "/dataInteractions";
	}
}
