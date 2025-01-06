using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;

//
public record IUser(string? _id, string? Email, string? PasswordHash, string? License_Key, string? TenantId, string? User_Id);
public record IAdmin(string _id, string? User, string[]? Users);
public record IAuth(string? AccessToken, string? RefreshToken);

//
public record ApiErrorResponse(string? Message, string? Code, object? Details);
//
public record ApiSuccessResponse<T>(T? Data, Meta? Meta);
public record Meta(string? Message, int? Page, int? Limit, int? Total, Dictionary<string, object>? AdditionalData);

//
public record AuthRecord(IUser? User, IAdmin? Admin, IAuth? Auth);
public record CookiesRecord<T>(string _id, string? TenantId, string? UserId, string? ProfileId, T[]? Cookies);

public record Doc<T>(
	string UserId,
	List<ApiObject<T>> Objects
);
public record ApiObject<T>(
    [property: JsonPropertyName("_id")] string Id,
		string Type,
    T Data
);

public record ObjectsCookies<T>(
	T[] Cookies,
	string ProfileId
);

