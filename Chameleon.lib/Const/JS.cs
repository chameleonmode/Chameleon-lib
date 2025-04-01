using System.Text.Json.Serialization;
using System.Text.Json;

namespace Chameleon.lib.Const;
public static class JS {
	public static readonly JsonSerializerOptions CamelCaseOptions = new() {
    WriteIndented = true, // Pretty print JSON
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase, //Use camelCase
	};
	public static readonly JsonSerializerOptions CaseInsensitiveOptions = new() {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
	};
	public static readonly JsonSerializerOptions InsensitiveCamelCaseOptions = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static T? DeserializeSafely<T>(string json) {
		try {
			return JsonSerializer.Deserialize<T>(json, InsensitiveCamelCaseOptions);
		} catch {
			return default;
		}
	}

	public static string? Serialize(object o, JsonSerializerOptions? options = null) => JsonSerializer.Serialize(o, options ?? InsensitiveCamelCaseOptions);
	

	public class DynamicJsonConverter<T1, T2> : JsonConverter<T2> where T1 : T2 {
		public override T2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
			return JsonSerializer.Deserialize<T1>(jsonObject.GetRawText(), options);
		}

		public override void Write(Utf8JsonWriter writer, T2 value, JsonSerializerOptions options) {
			JsonSerializer.Serialize(writer, value, options);
		}
	}
}
