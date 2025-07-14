using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chameleon.lib.Util;

public static class JSON {
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
	public static readonly JsonSerializerOptions EnumConverter = new() {
		WriteIndented = true,
		IncludeFields = true,
		Converters = { new JsonStringEnumConverter() },
	};
	public static readonly JsonSerializerOptions InsensitiveEnumConverter = new() {
		 PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

	public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) {
			return EX.Catch(()=> JsonSerializer.Deserialize<T>(json, options ?? InsensitiveCamelCaseOptions));
	}
	public static T Parse<T>(string? json, JsonSerializerOptions? options = null) {
		return Deserialize<T>(json ?? "", options) ?? Activator.CreateInstance<T>();
	}

	public static string Serialize(object o, JsonSerializerOptions? options = null) =>
	 JsonSerializer.Serialize(o, options ?? InsensitiveCamelCaseOptions);
	public static string? Stringify(object? o, JsonSerializerOptions? options = null) {
		return o is null ? null : Serialize(o, options);
	}

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
