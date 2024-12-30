using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;

public record ApiErrorResponse(string? Message, string? Code, object? Details);

public record ApiSuccessResponse<T>(T? Data, Meta? Meta);
public record Meta(int? Page, int? Limit, int? Total, Dictionary<string, object>? AdditionalData);

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

