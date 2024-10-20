namespace Chameleon.lib.Common.Interfaces.Sys;
public interface IAuthUser {
	long UserId { get; set; }
	long? CreatorUserId { get; set; }
	string UserName { get; set; }
	bool TookGuidedTour { get; set; }
}
public interface IAuthUserToken {
	string AuthToken { get; set; }
	bool HasAuthToken { get; }
	long ExpireInSeconds { get; set; }
	string EncryptedAccessToken { get; set; }
	string AuthRefreshToken { get; set; }
}
public interface IAuthLimits {
	ILimits Limits { get; set; }
}
public interface ILimits {
	bool HasOutreach { get; }
	bool HasYouTube { get; }
	bool HasWordPress { get; }
	int MaxProfilesCount { get; }
	IContentDiscoveryLimits ContentDiscoveryLimits { get; }
	int MaxAssistantsCount { get; }
}
public interface IContentDiscoveryLimits {
	bool HasProspector { get; }
	bool HasProspectorContent { get; }
	bool HasSocials { get; }
	bool HasSocialsContent { get; }
	int MaxRssCount { get; }
}
public interface IAuthPermissions {
	string[] Permissions { get; set; }
	bool CanCreateProfiles { get; set; }
}
[Obsolete("Added for compatibility with corrent infrastructure project until _authSession refactoed out only")]
public interface IAuthSession
			: IAuthUser
			, IAuthUserToken
			, IAuthLimits
			, IAuthPermissions
			, Chameleon.lib.Common.Interfaces.Systemics.ISingletonDependency {
}

