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

public record PlatformaticDBuser(
		string id,
		string userId,
		string fullName,
		string email,
		string provider,
		string providerId,
		string tenantId,
		string licenseKey,
		int isSuperUser,
		DateTime createdAt
);

#pragma warning restore IDE1006 // Naming Styles
