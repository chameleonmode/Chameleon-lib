namespace Chameleon.lib;
public record LoginSettings(string LoginName, string LicenseKey, bool AutoLogin = true);

#pragma warning disable IDE1006 // Naming Styles
public record TokenResponse(
		string access_token,
		string refresh_token,
		string id_token,
		string Scope,
		int expires_in,
		string token_type
);

public record TokenPayload(
		string iss,
		string sub,
		string[] aud,
		int iat,
		int exp,
		string scope,
		string azp,
		object[] permissions
);

public record PlatformaticReqError(
		string error,
		string message
);

public record AppClientInfo(
		string latest
);

public record PlatformaticUser(
		object id,
		string userId,
		string email,
		string licenseKey,
		string tenantId,
		string provider,
		string providerId,
		DateTime createdAt,
		DateTime updatedAt
);

public record PlatformaticDataInteraction(
		object id,
		string interactionId,
		string tenantId,
		string senderId,
		string receiverId,
		string dataType,
		string dataPayload,
		DateTime createdAt
);

public record PlatformaticDataPayload<T>(
	T payload
);

public record CookyPayload<T>(
	string profileId,
	T[] cookiesJs
);

//public record Cookiesj(
//	string name,
//	string value,
//	string domain,
//	string path,
//	int expires,
//	bool httpOnly,
//	bool secure,
//	int sameSite
//);


#pragma warning restore IDE1006 // Naming Styles
