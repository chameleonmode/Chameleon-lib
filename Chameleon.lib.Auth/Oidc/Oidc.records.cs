namespace Chameleon.lib.Auth.Oidc;
#pragma warning disable IDE1006 // Naming Styles
public record TokenResponse(
		string access_token,
		string id_token,
		string Scope,
		int expires_in,
		string token_type
);
#pragma warning restore IDE1006 // Naming Styles