using Chameleon.lib.Common;
using Chameleon.lib.Common.Util;
using Chameleon.lib.WebBrowser.Models;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.Tests.WebBrowser;
public class BrowserLauncherTests : WebBroswserTestsBase {

	[Fact]
	public async Task Test_LaunchBrowserInstance_Chrome()
	{
		_ = _tcs.Task;
		Assert.NotNull(SysBrowserServiceBase);

		IoC.SetJsonValue(new EmulationOptions {
			DisableWebRTC = true,
			SpoofClientRects = false,
			SpoofFontFingerprint = true,
			SpoofCanvasFingerprint = true,
			SpoofWebGLFingerprint = false,
			SpoofGeoLocation = true,
			AutoTimezone = true,
		}, nameof(EmulationOptions));
		var bi = await SysBrowserServiceBase.Open(new SysBrowserOpenOptions(SystemBrowserType.Chrome, new Common.Models.UserProfileModel() { Id = 123, Proxy = new Common.Models.ProxySettingsModel() }));
		Assert.NotNull(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_Brave()
	{
		_ = _tcs.Task;
		Assert.NotNull(SysBrowserServiceBase);

		var bi = await SysBrowserServiceBase.Open(new SysBrowserOpenOptions(SystemBrowserType.Brave, new Common.Models.UserProfileModel() { Id = 123, Proxy = new Common.Models.ProxySettingsModel() }));
		Assert.NotNull(bi);
	}

	[Fact]
	public async Task Test_LaunchBrowserInstance_FF()
	{
		_ = _tcs.Task;
		Assert.NotNull(SysBrowserServiceBase);

		IoC.SetJsonValue(new EmulationOptions {
			DisableWebRTC = true,
			SpoofClientRects = true,
			SpoofFontFingerprint = true,
			SpoofCanvasFingerprint = true,
			SpoofWebGLFingerprint = true,
			SpoofGeoLocation = true,
			AutoTimezone = true,
		}, nameof(EmulationOptions));

		//var bi = await SysBrowserServiceBase.Open(
		//	new SysBrowserOpenOptions(
		//		SystemBrowserType.Firefox,
		//		new Common.Models.UserProfileModel() {
		//			Id = 111
		//		})
		//	);
		//await ProUtil.TryKillProcess(bi!.Settings.Brocess);
		var bi = await SysBrowserServiceBase.Open(
			new SysBrowserOpenOptions(
				SystemBrowserType.Firefox,
				new Common.Models.UserProfileModel() {
					Id = 111,
					Proxy = new Common.Models.ProxySettingsModel() {
						Host = "proxy.chameleonmode.com",
						Port = 31112,
						UserName = "elimdadia_gmail_com",
						Password = "gb0Q1sXdTDZTlR2J_session-mk3wMyyY"
					}
				})
			);
		//Assert.NotNull(bi);
		//	_ = await SysBrowserServiceBase.Open(
		//new SysBrowserOpenOptions(
		//	Common.Enums.SystemBrowserType.Firefox,
		//	new Common.Models.UserProfileModel() {
		//		Id = 111,
		//		Proxy = new Common.Models.ProxySettings() {
		//			Host = "proxy.chameleonmode.com",
		//			Port = 31112,
		//			UserName = "elimdadia_gmail_com",
		//			Password = "gb0Q1sXdTDZTlR2J_session-mk3wMyyY"
		//		}
		//	})
		//);
		//_ = await SysBrowserServiceBase.Open
		//	(
		//	new SysBrowserOpenOptions(
		//		Common.Enums.SystemBrowserType.Firefox,
		//		new Common.Models.UserProfileModel() {
		//			Id = 222
		//		})
		//	);
	}
}
