using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;

public record ApiErrorResponse(
		string? Error,
		string? Code,
		object? Details
);

public record ApiSuccessResponse<T>(
		T? Data,
		Meta? Meta
);

public record Meta(
		int? Page,
		int? Limit,
		int? Total,
		Dictionary<string, object>? AdditionalData
);

public class BaseObject<T> {
	[JsonPropertyName("_id")]
	public required string Id { get; init; }
	public required string Type { get; init; }
	public required T Data { get; init; }
}

public class Doc<T> {
	public required string UserId { get; init; }
	public List<BaseObject<T>> Objects { get; init; } = new(capacity: 10);
}

public record TokenObject {
	public string? Token { get; init; }
}

public class CookieObject<T> {
	public T[]? Cookies { get; init; }
	public string? ProfileId { get; init; }
}

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
}
