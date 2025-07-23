
using Chameleon.lib.Browzio;
using Chameleon.lib.Browzio.Services.Browzas;

namespace Tests.WebBrowser;
public class BrowserLauncherTests : TestSetup {
	public override async Task InitializeAsync() {
		await base.InitializeAsync();
		await Browzio.I.Init();
	}

	readonly ManualResetEventSlim testCompletionEvent = new(false);
	void KeepAlive(IBrowserInstance bi) {
		// Start a monitoring task that will complete when the signal file is deleted
		_ = Task.Run(() => {
			try {
				while (bi.Brocess?.HasExited == false) {
					Thread.Sleep(1000 * 6);
				}
			} catch { }
			testCompletionEvent.Set();
		});

		// Wait for the manual signal (file deletion) or timeout after 30 minutes
		_ = testCompletionEvent.Wait(TimeSpan.FromMinutes(30));
	}

	// SGP6J3fr
	// CYpEvUqY
	// vUp6cZAY
	// mzBorsdy
	// N2Vb4Jvy
	[Fact]
	public async Task Test_LaunchBrowserInstance_Chrome() {
		var bi = await Browzio.I.Browzas.Open(Browzio.Factory.Chrome(new ("https://example.com") {
			Id = 22,
			Proxy = new BrowserProxy() {
				Host = "proxy.chameleonmode.com",
				Port = 31112,
				UserName = "elimdadia_gmail_com",
				Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
			},
			Emulations = new() {
				AutoTimezone = true,
				SpoofGeoLocation = true,
				SpoofWebGLFingerprint = true,
				SpoofCanvasFingerprint = true,
				SpoofFontFingerprint = true,
				SpoofAudio = true,
				SpoofClientRects = true
			},
		}));
		Assert.NotNull(bi);
		await Task.Delay(1000 * 3); // Wait for the browser to load
		await bi.Closee(); // Close the browser instance after testing
		// KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave() {
		var bi = await Browzio.I.Browzas.Open(Browzio.Factory.Brave(new ("https://example.com") {
			Id = 22,
			Proxy = new BrowserProxy() {
				Host = "proxy.chameleonmode.com",
				Port = 31112,
				UserName = "elimdadia_gmail_com",
				Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
			},
			Emulations = new() {
				AutoTimezone = true,
				SpoofGeoLocation = true,
				SpoofWebGLFingerprint = true,
				SpoofCanvasFingerprint = true,
				SpoofFontFingerprint = true,
				SpoofAudio = true,
				SpoofClientRects = true
			},
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_FF() {
		var bi = await Browzio.I.Browzas.Open(Browzio.Factory.Firefox(new ("https://example.com") {
			Id = 22,
			Proxy = new BrowserProxy() {
				Host = "proxy.chameleonmode.com",
				Port = 31112,
				UserName = "elimdadia_gmail_com",
				Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
			},
			Emulations = new() {
				AutoTimezone = true,
				SpoofGeoLocation = true,
				SpoofWebGLFingerprint = true,
				SpoofCanvasFingerprint = true,
				SpoofFontFingerprint = true,
				SpoofAudio = true,
				SpoofClientRects = true
			},
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}
}//http://127.0.0.1:3663/init?instanceId=22&sessionId=33a986d1-fa2b-4d22-bdd9-791117c48b33
