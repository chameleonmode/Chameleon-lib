using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Models;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.WebBrowser;
public class BrowserLauncherTests {
	// Create a manual reset event that will keep the test running
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
	//chrome-extension://onmphcpdlamnigcccfcpikhihfaffapp/data/web/register.html?sessionId=05bf7007-66cc-4e54-b01d-847942bfc37e&instanceId=3
	[Fact]
	public async Task Test_LaunchBrowserInstance_Chrome() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Chrome,
				new BrowserProfile() {
					Id = 8,
					Proxy = new BrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-CYpEvUqY"
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
					StartUrl = "https://example.com",
				})
		);
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Brave,
				new BrowserProfile() {
					Id = 8,
					Proxy = new BrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-CYpEvUqY"
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
					StartUrl = "https://example.com",
				})
		);
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

//chrome-extension://greckoleon@chameleonmode.com/data/web/register.html
//moz-extension://greckoleon@chameleonmode.com/data/web/register.html
	[Fact]
	public async Task Test_LaunchBrowserInstance_FF() {
		var bi = await SystemBrowserService.Instance.Open(new(SystemBrowserType.Firefox, 
			new() {
				Id = 18,
				// Proxy = new BrowserProxy() {
				// 	Host = "proxy.chameleonmode.com",
				// 	Port = 31112,
				// 	UserName = "elimdadia_gmail_com",
				// 	Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-CYpEvUqY"
				// },
				Emulations = new() {
					AutoTimezone = true,
					SpoofGeoLocation = true,
					SpoofWebGLFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofFontFingerprint = true,
					SpoofAudio = true,
					SpoofClientRects = true
				},
				StartUrl = "https://example.com",
			})
		);
		Assert.NotNull(bi);
		KeepAlive(bi);
	}
}
