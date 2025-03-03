using Chameleon.lib.Auth;
using Chameleon.lib.Const;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;
public class DB {
	DB() { }

	#region Models
	public record User(
		object Id,
		string UserId,
		string Email,
		string LicenseKey,
		string TenantId,
		string Provider,
		string ProviderId,
		DateTime CreatedAt,
		DateTime UpdatedAt
	);
	public record PlatformaticDataInteraction(
		object Id,
		string InteractionId,
		string TenantId,
		string SenderId,
		string ReceiverId,
		string DataType,
		string DataPayload,
		DateTime CreatedAt
)	;
	#endregion

	#region  Routes
	public const string DataInteractions = "/dataInteractions";
	public static class Routes {
		public static class License {
			public const string ROUTE = "/license";
			static object LicenseBody => new { license_key = Session.Instance.Login!.LicenseKey };
			public static Task<DB.User?> ActivateLicense => Client.Post<DB.User>($"{ROUTE}/activate",
				new(Body: LicenseBody)
			);

			public record Data(string License_key, string Purchase_id, int Product_id, int Status, object Guid);
			public static Task<Data?> KickLicenseData => Client.Post<Data>($"{ROUTE}/data",
				new(Body: LicenseBody)
			);
			
			public record Status(int Valid, int Active, object Guid);
			public static Task<Status?> KickLicenseStatus => Client.Post<Status>($"{ROUTE}/status",
				new(Body: LicenseBody)
			);

			public record Customer(bool Status, string Secret);
			public static Task<Customer?> KickCustomer => Client.Post<Customer>($"{ROUTE}/customer",
				new(Body: new { email = Session.Instance.Login!.LoginName })
			);
		}
		public static class User {
			public const string ROUTE = "/db/user";
			public static Task<DB.User?> GetDBuser => Client.Get<DB.User>($"{ROUTE}/", new(EnsureSuccess: false));
			public static Task<IEnumerable<DB.User>?> GetDBusers => Client.Get<IEnumerable<DB.User>>($"{ROUTE}/all");
			public static Task<DB.User?> GetAnyDBuser(string email) => Client.Get<DB.User>($"{ROUTE}/any",
				new(
					Q: $"?email={Uri.EscapeDataString(email)}",
					EnsureSuccess: false
				)
			);
			public static Task<IEnumerable<DB.User>?> CreateUser(string email) {
				return Client.Post<IEnumerable<DB.User>>($"{ROUTE}/", new(Q: $"?email={Uri.EscapeDataString(email)}"));
			}
		}
		public static class Cooky {
			public const string ROUTE = "/db/cooky";
			public static Task<PlatformaticDataInteraction?> SendCookies<T>(
				string email,
				string profileId,
				IReadOnlyList<T> cookiesJs
			) {
				return Client.Post<PlatformaticDataInteraction>($"{ROUTE}/",
					new(Body: new { email, payload = new { profileId, cookiesJs } })
				);
			}
		}
		public static class App {
			public const string ROUTE = "/app";

			public record AppClientInfo(string Latest);
			public static Task<AppClientInfo?> GetLatestVersion => Client.Get<AppClientInfo>($"{ROUTE}/latest",
				new(Q: $"?os={(OperatingSystem.IsMacOS() ? "mac" : "win")}", Authorize: false)
			);
			public static async Task<bool> DownloadLatest(Action<string> onProgress) {
				// Local path where the downloaded file will be saved
				var ext = OperatingSystem.IsMacOS() ? "zip" : "7z";
				// Send an asynchronous GET request and ensure headers are read before downloading the stream
				using var response = await Client.HttpClient.GetAsync($"{ROUTE}/download" + $"?ext={ext}", HttpCompletionOption.ResponseHeadersRead);
				_ = response.EnsureSuccessStatusCode();

				// Get the file name from the Content-Disposition header
				var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "Chameleon." + ext;
				var outputFile = Path.Combine(FilePaths.AppDownloadDir, fileName);

				// Get the total number of bytes (if available)
				var totalBytes = response.Content.Headers.ContentLength;
				var buffer = new byte[8192];
				double lastProgressReported = 0; // Tracks the last reported progress percentage
				long totalBytesRead = 0;
				int bytesRead;

				// Open a stream to write the downloaded content to a file
				using var contentStream = await response.Content.ReadAsStreamAsync();
				using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

				// Read the content stream in chunks
				while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0) {
					// Write the chunk to the file
					await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
					totalBytesRead += bytesRead;

					// Report progress only if totalBytes is available and we've passed the next 10% increment.
					if (totalBytes.HasValue) {
						var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
						if (progressPercentage - lastProgressReported >= 10 || progressPercentage >= 100) {
							lastProgressReported = Math.Floor(progressPercentage / 10) * 10;
							var progress = $"Downloaded {totalBytesRead} of {totalBytes.Value} bytes ({progressPercentage:0.00}%)";
							onProgress(progress);
						}
					} else {
						// If total size is unknown, report the raw byte count (or customize as needed)
						onProgress($"Downloaded {totalBytesRead} bytes");
					}
				}

				ProcessUtil.OpenFolder(FilePaths.AppDownloadDir);

				return File.Exists(outputFile);
			}
		}
	}
	#endregion

	#region Props
	public static DB Instance { get; } = new();
	public static Client Client => Client.Instance;
	//
	public User? DBuser { get; private set; }
	public IEnumerable<User>? DBusers { get; private set; }
	public Routes.App.AppClientInfo? LatestVersion { get; private set; }
	#endregion

	//Auth
	bool ranLicenseCheck = false;
	public async Task EnsureUser() {
		DBuser ??= await Routes.User.GetDBuser ?? await Routes.License.ActivateLicense;
		ArgumentNullException.ThrowIfNull(DBuser, "User not found");
		DBusers ??= await Routes.User.GetDBusers;
		//
		LatestVersion ??= await Routes.App.GetLatestVersion;
		// Double check license key if it's null
		// TODO: Remove this after all users have migrated to auth0
		if (!ranLicenseCheck && DBuser.LicenseKey == null) {
			DBuser = (await Routes.License.ActivateLicense) ?? DBuser;
			ranLicenseCheck = true;
		}
	}

	#region GET's
	public async Task<List<PlatformaticDataInteraction>?> GetDataInteractions() {
		await EnsureUser();
		return await Client.Get<List<PlatformaticDataInteraction>>(DataInteractions);
	}
	public async Task<IEnumerable<CookyPayload<T>>?> GetCookyDataInteractions<T>() {
		var interactions = await GetDataInteractions();
		return interactions?
			.Where(i => i.DataType == "cooky")
			.Select(i => JS.DeserializeSafely<CookyPayload<T>>(i.DataPayload))
			.Where(payload => payload != null)!;
	}
	#endregion

	#region POST's
	#endregion

	#region DELETE's
	public async Task DeleteDataInteractions() {
		var interactions = await GetDataInteractions();
		foreach (var interaction in interactions!) {
			_ = await Client.Delete<object>($"{DataInteractions}/{interaction.Id}");
		}
	}
	#endregion
}
