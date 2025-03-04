using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.Utils;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests : TestSetup {
	readonly DB platformaticDB = DB.Instance;

	public PlatformaticTests() : base(0) { }

	[Fact]
	public async Task Service_Routes_App() {
		var version = await Service.Routes.App.GetLatestVersion;
		Assert.NotNull(version);

		var success = await Service.Routes.App.DownloadLatest(Console.WriteLine);
		Assert.True(success);
	}

	[Fact]
	public async Task Service_Routes_Air() {
		var res = await Service.Routes.Air.Ask(new(
				"reddit",
				new {
					keyword = "mushroom",
				}
			)
		);
		Assert.NotNull(res?.Payload);
	}

	[Fact]
	public async Task DB_Routes_License() {
		var customer = await DB.Routes.License.KickCustomer;
		Assert.NotNull(customer);

		var data = await DB.Routes.License.KickLicenseData;
		Assert.NotNull(data);

		var status = await DB.Routes.License.KickLicenseStatus;
		Assert.NotNull(status);

		var user = await DB.Routes.License.ActivateLicense;
		Assert.NotNull(user);
	}

	[Fact]
	public async Task DB_Routes_User() {
		var user = await DB.Routes.User.GetDBuser;
		Assert.NotNull(user);

		var users = await DB.Routes.User.GetDBusers;
		Assert.NotNull(users);

		var email = "1@example.com";
		var create = await DB.Routes.User.CreateUser(email);
		Assert.NotNull(create);
		var any = await DB.Routes.User.GetAnyDBuser(email);
		Assert.NotNull(any);
	}

		[Fact]
	public async Task DB_Routes_Cooky() {
		var cookies = await PlaywrightUtil.GetCookies(new(new(Enums.SystemBrowserType.Chrome, new() { Id = 25541 }), null));
		var email = "elimdadia@gmail.com";
		//var email = "ezexerael@gmail.com";
		var data = await DB.Routes.Cooky.SendCookies(email, "25541", cookies);
		Assert.NotNull(data);

		var cooky = await DB.Routes.Cooky.GetCookies<BrowserContextCookiesResult>();
		Assert.NotNull(cooky);
		Assert.NotEmpty(cooky);
	}

	[Fact]
	public async Task DB_EnsureUser() {
		await platformaticDB.EnsureUser();
		Assert.NotNull(platformaticDB.DBuser);
		Assert.NotNull(platformaticDB.DBusers);
		Assert.NotEmpty(platformaticDB.DBusers);
	}

	[Fact]
	public async Task DB_GetDataInteractions() {
		var datas = await platformaticDB.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_PostDataInteraction() {
		var datas = await platformaticDB.PostDataInteraction(new(
			ReceiverId: "568bea38-bbc8-4070-a4aa-8ae6f0fdcd4b", 
			DataType: "poop",
			DataPayload: "poop"
		));
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_DeleteDataInteractions() {
		await platformaticDB.DeleteDataInteractions(DB.Routes.Cooky.DataType);
		var data = await platformaticDB.GetDataInteractions(DB.Routes.Cooky.DataType);
		Assert.Empty(data!);
	}
}
