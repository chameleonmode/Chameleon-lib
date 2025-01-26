using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Auth;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.Services;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests(int dictionary = 0) : TestSetup(dictionary) {
	readonly PlatformaticDB platformaticDB = PlatformaticDB.Instance;
	readonly PlaywrightCookiesSyncService playCookySyncServive = PlaywrightCookiesSyncService.Instance;

	[Fact]
	public async Task Login_Success() {
		await platformaticDB.Login();
		Assert.NotNull(platformaticDB.DBuser);
	}

	[Fact]
	public async Task PutCookies_Seccess() {
		var cookies = await playCookySyncServive.GetCookies("25541", Enums.SystemBrowserType.Chrome);
		var data = await platformaticDB.SendCookies("1@1", "25541", cookies);
		Assert.NotNull(data);
	}

	[Fact]
	public async Task GetDataInteractions_Seccess() {
		var datas = await platformaticDB.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task GetDataInteractions_ToCookies_Seccess() {
		var datas = await platformaticDB.GetCookyDataInteractions<BrowserContextCookiesResult>();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DeleteDataInteractions_Seccess() {
		await platformaticDB.DeleteDataInteractions();
	}
}
