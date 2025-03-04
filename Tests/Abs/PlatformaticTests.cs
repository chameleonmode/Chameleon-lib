using System.Diagnostics;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Const;
using Chameleon.lib.Playwright.Utils;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests : TestSetup {
	readonly DB platformaticDB = DB.Instance;

	public PlatformaticTests() : base(0) { }

	[Fact]
	public async Task DB_Tags() {
		await DB.Instance.EnsureUser();
		// Create a tag
		var create = await DB.Instance.CreateTag("test", new Dictionary<string, List<string>> {
						{ "folder", new List<string> { "id", "id2" } }
				});
		Assert.NotNull(create);
		Assert.Equal("test", create.Name);

		// Get the tag
		var get = await DB.Instance.GetTag(create.Id);
		Assert.NotNull(get);
		var tag = new TagDto(get.Name, JS.DeserializeSafely<Dictionary<string, List<string>>>(get.Items)!);
		Assert.Equal("test", tag.Name);
		Assert.Contains("folder", tag.Items.Keys);
		Assert.Contains("id", tag.Items["folder"]);
		Assert.Contains("id2", tag.Items["folder"]);

		// Update the tag
		var update = await DB.Instance.UpdateTag(create.Id, "test", new Dictionary<string, List<string>> {
						{ "folder", new List<string> { "id", "id2", "id3" } }
				});
		Assert.NotNull(update);

		// Verify update
		get = await DB.Instance.GetTag(create.Id);
		tag = new TagDto(get!.Name, JS.DeserializeSafely<Dictionary<string, List<string>>>(get.Items)!);
		Assert.Contains("id3", tag.Items["folder"]);

		// List all tags
		var list = (await DB.Instance.GetTags())?.Select(
				t => new TagDto(t.Name, JS.DeserializeSafely<Dictionary<string, List<string>>>(t.Items)!)
		);
		Assert.NotNull(list);
		Assert.NotEmpty(list);
		Assert.Contains(list, t => t.Name == "test");

		// Search for tags
		var searchResults = await DB.Instance.SearchTags("te");
		Assert.NotNull(searchResults);
		Assert.Contains(searchResults, t => t.Name == "test");

		// Clean up
		await DB.Instance.DeleteTag(create.Id);
		get = await DB.Instance.GetTag(create.Id);
		Assert.Null(get);
	}

	[Fact]
	public async Task DB_ItemTags() {
		await DB.Instance.EnsureUser();

		// Create a tag first
		var tag = await DB.Instance.CreateTag("testTag", new Dictionary<string, List<string>>());
		Assert.NotNull(tag);

		// Create an item tag
		var itemTag = await DB.Instance.CreateItemTag("document", "doc123", "testTag");
		Assert.NotNull(itemTag);
		Assert.Equal("document", itemTag.TagItemType);
		Assert.Equal("doc123", itemTag.TagItemId);
		Assert.Equal("testTag", itemTag.TagName);

		// Get the item tag
		var getItemTag = await DB.Instance.GetItemTag("document", "doc123", "testTag");
		Assert.NotNull(getItemTag);
		Assert.Equal("document", getItemTag.TagItemType);
		Assert.Equal("doc123", getItemTag.TagItemId);
		Assert.Equal("testTag", getItemTag.TagName);

		// Get item tags for item
		var itemTags = await DB.Instance.GetItemTagsForItem("document", "doc123");
		Assert.NotNull(itemTags);
		Assert.NotEmpty(itemTags);
		Assert.Contains(itemTags, it => it.TagName == "testTag");

		// Update the tag
		var update = await DB.Instance.UpdateTag(tag.Id, "testTag", new Dictionary<string, List<string>> {
						{ "document", new List<string> { "doc123" } }
				});
		Assert.NotNull(update);

		// Update the tag's items collection
		var updatedTag = await DB.Instance.GetTag(tag.Id);
		var itemsDict = JS.DeserializeSafely<Dictionary<string, List<string>>>(updatedTag!.Items) ?? new();
		Assert.Contains("document", itemsDict.Keys);
		Assert.Contains("doc123", itemsDict["document"]);

		// Create a second item tag
		var itemTag2 = await DB.Instance.CreateItemTag("document", "doc456", "testTag");
		Assert.NotNull(itemTag2);

		// Get all item tags
		var allItemTags = await DB.Instance.GetItemTags();
		Assert.NotNull(allItemTags);
		Assert.Contains(allItemTags, it => it.TagItemId == "doc123" && it.TagName == "testTag");
		Assert.Contains(allItemTags, it => it.TagItemId == "doc456" && it.TagName == "testTag");

		// Delete item tag
		await DB.Instance.DeleteItemTag("document", "doc123", "testTag");
		var afterDelete = await DB.Instance.GetItemTag("document", "doc123", "testTag");
		Assert.Null(afterDelete);

		// Clean up
		await DB.Instance.DeleteTag(tag.Id);
		var getTag = await DB.Instance.GetTag(tag.Id);
		Assert.Null(getTag);
	}

	[Fact]
	public async Task Service_Routes_App() {
		var version = await Service.Routes.App.GetLatestVersion;
		Assert.NotNull(version);

		var success = await Service.Routes.App.DownloadLatest((progress) => {
			Debug.WriteLine(progress);
		});
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
		Assert.NotNull(res!.Payload);

		res = await Service.Routes.Air.Ask(new(
				Feature: "reddit",
				Scenario: new {
					keyword = "barkley",
				},
				Background: "sarcastic-ish"
			)
		);
		Assert.NotNull(res!.Payload);

		res = await Service.Routes.Air.Ask(new(
			Feature: "reddit",
			Scenario: new {
				keyword = "soup",
			},
			Background: "default"
			)
		);
		Assert.NotNull(res!.Payload);
	}

	[Fact]
	public async Task DB_Routes_License() {
		var customer = await DB.Routes.License.KickCustomer;
		Assert.NotNull(customer);

		var data = await DB.Routes.License.KickLicenseData;
		Assert.NotNull(data);

		var status = await DB.Routes.License.KickLicenseStatus;
		Assert.NotNull(status);

		var user = await DB.Routes.License.Update;
		Assert.NotNull(user);
	}

	[Fact]
	public async Task DB_Routes_User() {
		var user = await DB.Routes.User.GetDBuser;
		Assert.NotNull(user);

		var email = "1@example.com";
		var create = await DB.Routes.User.CreateUser(email);
		Assert.NotNull(create);
		var any = await DB.Routes.User.GetAnyDBuser(email);
		Assert.NotNull(any);

		var users = await DB.Routes.User.GetDBusers;
		Assert.NotNull(users);
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
	}

	[Fact]
	public async Task DB_GetDataInteractions() {
		var datas = await platformaticDB.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_PostDataInteraction() {
		var datas = await platformaticDB.PostDataInteraction(new(
			ReceiverId: "d65f225e-8e42-45f3-8d2f-5e001fce630d",
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

		await platformaticDB.DeleteDataInteractions();
		var all = await platformaticDB.GetDataInteractions();
		Assert.Empty(all!);
	}
}
