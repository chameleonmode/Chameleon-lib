using Chameleon.lib;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.Common.Models;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.WebBrowser;
public class BrowserLauncherTests {

	void KeepAlive(ISysBrowserInstance bi){
				// Create a manual reset event that will keep the test running
    var testCompletionEvent = new ManualResetEventSlim(false);

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
				new SysBrowserProfile() {
					Id = 5,
					// Proxy = new SysBrowserProxy() {
					// 	Host = "proxy.chameleonmode.com",
					// 	Port = 31112,
					// 	UserName = "elimdadia_gmail_com",
					// 	Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-vUp6cZAY"
					// }
				}),
				"https://browserleaks.com",
				new () {
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofAudio = true,
					SpoofNavigator = false,
					SpoofGeoLocation = true,
					AutoTimezone = true,
				}
		);
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Brave,
				new SysBrowserProfile() {
					Id = 3,
					Proxy = new SysBrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-CYpEvUqY"
					}
				}),
				"https://example.com",
				new () {
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofAudio = true,
					SpoofNavigator = false,
					SpoofGeoLocation = true,
					AutoTimezone = true,
				}
		);
		Assert.NotNull(bi);
		KeepAlive(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_FF() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Firefox,
				new SysBrowserProfile() {
					Id = 3,
					Proxy = new SysBrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
					}
				}),
				"https://example.com",
				new () {
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofAudio = true,
					SpoofNavigator = false,
					SpoofGeoLocation = true,
					AutoTimezone = true,
				}
		);
		Assert.NotNull(bi);
	}
}
