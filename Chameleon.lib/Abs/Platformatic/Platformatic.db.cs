using Chameleon.lib.Auth;
using Chameleon.lib.Const;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs.Platformatic;
public class PlatformaticDB {
	readonly Session session;
	readonly AbsClient absClient;

	bool ranLicenseCheck = false;

	PlatformaticDB() {
		session = Session.Instance;
		absClient = new AbsClient(Configs.Urls.ABS_PLATFORMATIC_BASE_URL, session.Authenticate);
	}

	#region Props
	//
	Task<PlatformaticUser?> GetDBuser =>
		absClient.Get<PlatformaticUser>(Configs.Endpoints.DB.USER,
			new(
				Q: $"?email={Uri.EscapeDataString(session.Login!.LoginName)}", EnsureSuccess: false
			)
		);
	public PlatformaticUser? DBuser { get; private set; }
	//
	Task<IEnumerable<PlatformaticUser>?> GetDBusers =>
		absClient.Get<IEnumerable<PlatformaticUser>>(Configs.Endpoints.Users);
	public IEnumerable<PlatformaticUser>? DBusers { get; private set; }
	// 
	public Task<PlatformaticUser?> ValidateLicese =>
		absClient.Post<PlatformaticUser>(Configs.Endpoints.LICENSE.ACTIVATE,
			new(
				Body: new { license_key = session.Login!.LicenseKey }
			)
		);// 
	public Task<AppClientInfo?> GetLatestVersion =>
		absClient.Get<AppClientInfo>(Configs.Endpoints.APP.LATEST,
			new(Q: $"?os={(OperatingSystem.IsMacOS() ? "mac" : "win")}", Authorize: false)
		);
	public AppClientInfo? LatestVersion { get; private set; }
	#endregion

	//Auth
	public async Task EnsureUser() {
		DBuser ??= await GetDBuser ?? await ValidateLicese;
		ArgumentNullException.ThrowIfNull(DBuser, "User not found");
		DBusers ??= await GetDBusers;
		//
		LatestVersion ??= await GetLatestVersion;
		// Double check license key if it's null
		// TODO: Remove this after all users have migrated to auth0
		if (!ranLicenseCheck && DBuser.licenseKey == null) {
			DBuser = (await ValidateLicese) ?? DBuser;
			ranLicenseCheck = true;
		}
	}

	#region GET's
	public async Task<List<PlatformaticDataInteraction>?> GetDataInteractions() {
		await EnsureUser();
		return await absClient.Get<List<PlatformaticDataInteraction>>(Configs.Endpoints.DataInteractions);
	}
	public async Task<IEnumerable<CookyPayload<T>>?> GetCookyDataInteractions<T>() {
		var interactions = await GetDataInteractions();
		return interactions?
			.Where(i => i.dataType == "cooky")
			.Select(i => JS.DeserializeSafely<CookyPayload<T>>(i.dataPayload))
			.Where(payload => payload != null)!;
	}
	public async Task<bool> DownloadLatest(Action<string> onProgress) {
		//await EnsureUser();

		// Local path where the downloaded file will be saved
		var ext = OperatingSystem.IsMacOS() ? "zip" : "7z";
		// Send an asynchronous GET request and ensure headers are read before downloading the stream
		using var response = await absClient.HttpClient.GetAsync(Configs.Endpoints.APP.DOWNLOAD + $"?ext={ext}", HttpCompletionOption.ResponseHeadersRead);
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
	#endregion

	#region POST's
	public async Task CreateUser(string email) {
		await EnsureUser();

		DBusers = await absClient.Post<IEnumerable<PlatformaticUser>?>(Configs.Endpoints.DB.USER,
			new(Body: new { email })
		);
	}
	public async Task<PlatformaticDataInteraction?> SendCookies<T>(
			string receiverEmail,
			string profileId,
			IReadOnlyList<T> cookiesJs) {
		await EnsureUser();
		return await absClient.Post<PlatformaticDataInteraction>(Configs.Endpoints.DB.COOKIES,
			new(
				Body: new { receiverEmail, payload = new { profileId, cookiesJs } }
			)
		);
	}
	#endregion

	#region DELETE's
	public async Task DeleteDataInteractions() {
	  var interactions = await GetDataInteractions();
		foreach (var interaction in interactions!) {
			_ = await absClient.Delete<object>($"{Configs.Endpoints.DataInteractions}/{interaction.id}");
		}
	}
	#endregion

	public static PlatformaticDB Instance { get; } = new();
}
