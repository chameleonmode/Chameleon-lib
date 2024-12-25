using System.Text.Json.Serialization;

namespace Chameleon.lib.Abs;

public enum UserType {
	TOKEN,
}
public enum ObjectType {
	COOKIE,
	CUSTOM
}

public static class ObjectTypes
{
  public static class USER
  {
    public static string GetUserType(UserType userType) => userType switch
    {
      UserType.TOKEN => "TOKEN",
      _ => throw new ArgumentOutOfRangeException(nameof(userType), userType, null)
    };
  }

  public static class OBJECT
  {
    public static string GetObjectType(ObjectType objectType) => objectType switch
    {
      ObjectType.COOKIE => "COOKIE",
      ObjectType.CUSTOM => "CUSTOM",
      _ => throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null)
    };
  }
}
public static class IoCKeys {
	public static string TokenObject => nameof(Abs) + nameof(Abs.TokenObject);
}

public class ApiErrorResponse {
	public string? Error { get; set; }
	public string? Code { get; set; }
	public object? Details { get; set; }
}

public class ApiSuccessResponse<T> {
	public T? Data { get; set; }
	public Meta? Meta { get; set; }
}

public class Meta {
	public int? Page { get; set; }
	public int? Limit { get; set; }
	public int? Total { get; set; }
	public Dictionary<string, object>? AdditionalData { get; set; }
}

public class BaseObject<T> {
	[JsonPropertyName("_id")]
	public required string Id { get; set; }
	public required string Type { get; set; }
	public required T Data { get; set; }
}

public class Doc<T> {
	public required string UserId { get; set; }
	public List<BaseObject<T>> Objects { get; set; } = [];
}

public class TokenObject {
	public string? Token { get; set; }
}

public class CookieObject<T> {
	public T[]? Cookies { get; set; }
	public string? ProfileId { get; set; }
}
