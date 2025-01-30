using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests : TestSetup {
	readonly PlatformaticDB platformaticDB = PlatformaticDB.Instance;
	[Fact]
	public async Task EnsureUser_Success() {
		await platformaticDB.EnsureUser();
		Assert.NotNull(platformaticDB.DBuser);
		Assert.NotEmpty(platformaticDB.DBusers);
	}

	[Fact]
	public async Task ValidateLicese_Success() {
		var user = await platformaticDB.ValidateLicese;
		Assert.NotNull(user);
	}

	[Fact]
	public async Task CreateUser_Success() {
		await platformaticDB.CreateUser("6@example.com");
		Assert.NotNull(platformaticDB.DBusers);
	}

	[Fact]
	public async Task SendCookies_Success() {
		var cookies = await PlaywrightUtil.GetCookies("25541", Enums.SystemBrowserType.Chrome);
		var data = await platformaticDB.SendCookies("elimdadia@gmail.com", "25541", cookies);
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
