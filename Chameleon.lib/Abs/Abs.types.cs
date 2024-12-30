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

	public static class IoCKeys {
		public static string UserToken => $"{nameof(Abs)}{nameof(UserType)}{UserType.TOKEN}";
	}
}
