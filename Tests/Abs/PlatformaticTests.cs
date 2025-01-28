using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests(int dictionary = 0) : TestSetup(dictionary) {
	readonly PlatformaticDB platformaticDB = PlatformaticDB.Instance;
	[Fact]
	public async Task EnsureUser_Success() {
		await platformaticDB.EnsureUser();
		Assert.NotNull(platformaticDB.DBuser);
		Assert.NotEmpty(platformaticDB.DBusers);
	}

	[Fact]
	public async Task SendCookies_Success() {
		var cookies = await PlaywrightUtil.GetCookies("25541", Enums.SystemBrowserType.Chrome);
		var data = await platformaticDB.SendCookies("1@1", "25541", cookies);
		Assert.NotNull(data);
	}

	[Fact]
	public async Task GetDataInteractions_Success() {
		var datas = await platformaticDB.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task GetDataInteractions_ToCookies_Success() {
		var datas = await platformaticDB.GetCookyDataInteractions<BrowserContextCookiesResult>();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DeleteDataInteractions_Success() {
		await platformaticDB.DeleteDataInteractions();
	}
}
