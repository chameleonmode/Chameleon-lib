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
	public static bool IsNot(this string? self) =>
		self != null && self != string.Empty && !string.IsNullOrEmpty(self) && !string.IsNullOrWhiteSpace(self);
	public static bool Is(this string? self) =>
		self == null || self == string.Empty || string.IsNullOrEmpty(self) || string.IsNullOrWhiteSpace(self);
}
