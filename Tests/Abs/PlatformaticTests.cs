using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Auth;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.Services;

namespace Tests.Abs;
public class PlatformaticTests(int dictionary = 0) : TestSetup(dictionary) {
	readonly PlatformaticDB platformaticDB = PlatformaticDB.Instance;
	readonly PlaywrightCookiesSyncService playCookySyncServive = PlaywrightCookiesSyncService.Instance;
	[Fact]
	public async Task PutCookies_Seccess() {
		var cookies = await playCookySyncServive.GetCookies("25541", Enums.SystemBrowserType.Chrome);
		await platformaticDB.AddCookies("898", "25541", cookies);
	}

	[Fact]
	public async Task GetDBuser_Seccess() {
		var user = await platformaticDB.GetDBuser();
		Assert.NotNull(user);
	}
}
