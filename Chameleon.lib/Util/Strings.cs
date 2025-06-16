using System.Security.Cryptography;
using System.Text;

namespace Chameleon.lib.Util;

public static class StringsUtil {
	// Methods
	public static string GenerateRandomString(int length = 32) {
		var bytes = new byte[length];
		using (var rng = RandomNumberGenerator.Create()) {
			rng.GetBytes(bytes);
		}
		return Convert.ToBase64String(bytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');
	}

	public static string GenerateCodeChallenge(string codeVerifier) {
		var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
		return Convert.ToBase64String(challengeBytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');
	}

	// Extension Methods
	public static bool Is(this string? self) =>
		self == null || self == string.Empty || string.IsNullOrEmpty(self) || string.IsNullOrWhiteSpace(self);
	public static void ThrowIfNullOrEmpty(this string? self) => ArgumentException.ThrowIfNullOrEmpty(self);
	public static bool IsNot(this string? self) =>
		!self.Is();
	public static string Strip(this string self, string prefix) =>
		self.StartsWith(prefix) ? self[prefix.Length..] : self;

	public static object? ParseValue(this string? value) {
		// Try to parse the value as a simple type
		if (int.TryParse(value, out var intValue)) return intValue;
		if (bool.TryParse(value, out var boolValue)) return boolValue;
		if (double.TryParse(value, out var doubleValue)) return doubleValue;
		if (DateTime.TryParse(value, out var dateTimeValue)) return dateTimeValue;

		// If parsing fails, return the original string
		return value;
	}

}
