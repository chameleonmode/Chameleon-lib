using Chameleon.lib;
using Chameleon.lib.Common.Models;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.WebBrowser;
public class BrowserLauncherTests {
	
// http://proxy.chameleonmode.com:31112:elimdadia_gmail_com:gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr
// http://proxy.chameleonmode.com:31112:elimdadia_gmail_com:gb0Q1sXdTDZTlR2J_country-UnitedStates_session-CYpEvUqY
// http://proxy.chameleonmode.com:31112:elimdadia_gmail_com:gb0Q1sXdTDZTlR2J_country-UnitedStates_session-vUp6cZAY
// http://proxy.chameleonmode.com:31112:elimdadia_gmail_com:gb0Q1sXdTDZTlR2J_country-UnitedStates_session-mzBorsdy
// http://proxy.chameleonmode.com:31112:elimdadia_gmail_com:gb0Q1sXdTDZTlR2J_country-UnitedStates_session-N2Vb4Jvy
	[Fact]
	public async Task Test_LaunchBrowserInstance_Chrome() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Chrome,
				new SysBrowserProfile() {
					Id = 3,
					Proxy = new SysBrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-SGP6J3fr"
					}
				}),
				() => "https://example.com",
				new () {
					DisableWebRTC = true,
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofGeoLocation = true,
					AutoTimezone = true,
				}
		);
		Assert.NotNull(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(SystemBrowserType.Brave,
				new SysBrowserProfile() {
					Id = 1,
					// Proxy = new SysBrowserProxy() {
					// 	Host = "proxy.chameleonmode.com",
					// 	Port = 31112,
					// 	UserName = "elimdadia_gmail_com",
					// 	Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-vUp6cZAY"
					// }
				}),
				() => "https://example.com",
				new () {
					DisableWebRTC = true,
					SpoofClientRects = true,
					SpoofFontFingerprint = true,
					SpoofCanvasFingerprint = true,
					SpoofWebGLFingerprint = true,
					SpoofGeoLocation = true,
					AutoTimezone = true,
				}
		);
		Assert.NotNull(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_FF() {
		IoC.SetJsonValue(new EmulationOptions {
			DisableWebRTC = true,
			SpoofClientRects = true,
			SpoofFontFingerprint = true,
			SpoofCanvasFingerprint = true,
			SpoofWebGLFingerprint = true,
			SpoofGeoLocation = true,
			AutoTimezone = true,
		}, nameof(EmulationOptions));
		var bi = await  SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(
				SystemBrowserType.Firefox,
				new SysBrowserProfile() {
					Id = 2
				})
				, ()=>"https://example.com"
			);
	}
}
