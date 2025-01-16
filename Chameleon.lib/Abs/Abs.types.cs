using System.Runtime.Serialization;

namespace Chameleon.lib.Abs;

public enum UserType {
	TOKEN
}

public enum ObjectType {
  COOKIE,
	CUSTOM
}

public static class Constas {
	public static string ABS_BASE_URL =>
#if DEBUG
					"http://localhost:3001"
#else
            "https://abswebapp.azurewebsites.net"
#endif
	;

	public static class Endpoints {
		public const string Auth = "/auth";
		public const string Cookies = "/api/cookies";
	}

	public static class IoCKeys {
		public const string IAuth = $"{nameof(ABService)}{nameof(IAuth)}v3";
		public const string ITennant = $"{nameof(ABService)}{nameof(ITennant)}";
	}
}
