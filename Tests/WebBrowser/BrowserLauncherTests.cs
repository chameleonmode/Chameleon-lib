using Chameleon.lib;
using Chameleon.lib.Common.Models;
using Chameleon.lib.WebBrowser.Services;
using static Chameleon.lib.Common.Constants.Enums;

namespace Tests.WebBrowser;
public class BrowserLauncherTests {
	
	[Fact]
	public async Task Test_LaunchBrowserInstance_Chrome() {
		var bi = await SystemBrowserService.Instance.Open(
			new SysBrowserOpenOptions(
				SystemBrowserType.Chrome,
				new SysBrowserProfile() {
					Id = 28296,
					Proxy = new SysBrowserProxy() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_country-UnitedStates_session-MAa3x0NK"
					}
				}),
				() => "https://example.com",
				new EmulationOptions {
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
				SystemBrowserType.Brave,
				new SysBrowserProfile() {
					Id = 1,
					Proxy = new SysBrowserProxy()
				})
				, ()=>"https://example.com"
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
