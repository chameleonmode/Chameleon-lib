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
		var bi = await Browzio.I.Browzas.Launch(Browzio.Factory.Chrome(new("https://browserleaks.com/ip") {
			Id = 24,
			Proxy = new(
				"proxy.chameleonmode.com",
				31112,
				"elimdadia_gmail_com",
				"gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
			),
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Vivaldi() {
		var bi = await Browzio.I.Browzas.Launch(Browzio.Factory.BrowserSettings(BrowserType.Vivaldi, new("https://browserleaks.com/ip") {
			Id = 26,
			Proxy = new(
				host: "proxy.chameleonmode.com",
				port: 31112,
				userName: "elimdadia_gmail_com",
				password: "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
			),
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave() {
		var bi = await Browzio.I.Browzas.Launch(Browzio.Factory.Brave(new("https://browserleaks.com/ip") {
			Id = 22,
			Proxy = new BrowserProxy("proxy.chameleonmode.com", 31112, "elimdadia_gmail_com", "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"),
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_FF() {
		var bi = await Browzio.I.Browzas.Launch(Browzio.Factory.Firefox(new("https://browserleaks.com/ip") {
			Id = 22,
			Proxy = new BrowserProxy("proxy.chameleonmode.com", 31112, "elimdadia_gmail_com", "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"),
		}));
		Assert.NotNull(bi);
		KeepAlive(bi);
	}
}