using System.Diagnostics;
using Chameleon.lib;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Api.Dto;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.WebBrowser;

using Microsoft.Playwright;

namespace Tests.Abs;
public class PlatformaticTests : TestSetup {	
	public PlatformaticTests() : base(0) { }

	[Fact]
	public async Task DB_CheckKey() {
		var user = await DB.Post<DB.Routes.License.Status>($"{DB.Routes.License.prefix}/update",
				new(Body: new {
					license_key = "",
					email = ""
				}));
		Assert.NotNull(user);
	}

	#region DB users and data interactiions
	[Fact]
	public async Task DB_EnsureUser() {
		await DB.Instance.EnsureUser();
		Assert.NotNull(DB.Instance.DBuser);
		Assert.NotNull(DB.Instance.DBusers);
	}

	[Fact]
	public async Task DB_GetDataInteractions() {
		await DB.Instance.EnsureUser();
		var datas = await DB.Instance.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_PostDataInteraction() {
		await DB.Instance.EnsureUser();
		var datas = await DB.Instance.PostDataInteraction(new(
			ReceiverId: "ef61cf83-13e7-486a-ac37-84ec78841b4f",
			DataType: "poop",
			DataPayload: "poop"
		));
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_DeleteDataInteractions() {
		await DB.Instance.EnsureUser();
		await DB.Instance.DeleteDataInteractions(DB.Routes.Cooky.DataType);
		var data = await DB.Instance.GetDataInteractions(DB.Routes.Cooky.DataType);
		Assert.Empty(data!);

		await DB.Instance.DeleteDataInteractions();
		var all = await DB.Instance.GetDataInteractions();
		Assert.Empty(all!);
	}
	#endregion

	[Fact]
	public async Task DB_Tags() {
		await DB.Instance.EnsureUser();
		// Create a tag
		var name = "tester8";
		var create = await DB.Instance.CreateTag(name, []);
		Assert.NotNull(create);
		Assert.Equal(name, create.Name);

		// Get the tag by id
		var get = await DB.Instance.GetTag(create.Id);
		Assert.NotNull(get);
		Assert.Equal(name, get.Name);

		// Get the tag
		var getBy = await DB.Instance.GetTagBy(name);
		Assert.NotNull(getBy);
		Assert.Equal(getBy.Name, get.Name);

		// Update the tag
		var update = await DB.Instance.UpdateTag(create.Id, name, new Dictionary<string, List<string>> {
						{ "folder", new List<string> { "id", "id2", "id3" } }
				});
		Assert.NotNull(update);
		Assert.Equal(name, update.Name);

		var tag = new TagDto(update.Name, JSON.Deserialize<Dictionary<string, List<string>>>(update.Items)!);
		Assert.Contains("folder", tag.Items.Keys);
		Assert.Contains("id", tag.Items["folder"]);
		Assert.Contains("id2", tag.Items["folder"]);

		// Verify update
		get = await DB.Instance.GetTag(create.Id);
		tag = new TagDto(get!.Name, JSON.Deserialize<Dictionary<string, List<string>>>(get.Items)!);
		Assert.Contains("id3", tag.Items["folder"]);

		// List all tags
		var list = (await DB.Instance.GetTags())?.Select(
				t => new TagDto(t.Name, JSON.Deserialize<Dictionary<string, List<string>>>(t.Items)!)
		);
		Assert.NotNull(list);
		Assert.NotEmpty(list);
		Assert.Contains(list, t => t.Name == name);

		// Search for tags
		var searchResults = await DB.Instance.SearchTags("te");
		Assert.NotNull(searchResults);
		Assert.Contains(searchResults, t => t.Name == name);

		// Clean up
		await DB.Instance.DeleteTag(create.Id);
		get = await DB.Instance.GetTag(create.Id);
		Assert.Null(get);
	}

	[Fact]
	public async Task DB_ItemTags() {
		await DB.Instance.EnsureUser();

		var name = "testTagingz5";
		// Create a tag first
		var tag = await DB.Instance.CreateTag(name, []);
		Assert.NotNull(tag);

		// Create an item tag
		var itemTag = await DB.Instance.CreateItemTag("document", "doc123", name);
		Assert.NotNull(itemTag);
		Assert.Equal("document", itemTag.TagItemType);
		Assert.Equal("doc123", itemTag.TagItemId);
		Assert.Equal(name, itemTag.TagName);

		// Get the item tag
		var getItemTag = await DB.Instance.GetItemTagBy("document", "doc123", name);
		Assert.NotNull(getItemTag);
		Assert.Equal("document", getItemTag.TagItemType);
		Assert.Equal("doc123", getItemTag.TagItemId);
		Assert.Equal(name, getItemTag.TagName);

		// Get item tags for item
		var itemTags = await DB.Instance.GetItemTagsForItem("document", "doc123");
		Assert.NotNull(itemTags);
		Assert.NotEmpty(itemTags);
		Assert.Contains(itemTags, it => it.TagName == name);

		// Update the tag's items collection
		var update = await DB.Instance.UpdateTag(tag.Id, name, new Dictionary<string, List<string>> {
			{ "document", new List<string> { "doc123" } }
		});
		Assert.NotNull(update);
		var updatedTag = await DB.Instance.GetTag(tag.Id);
		var itemsDict = JSON.Deserialize<Dictionary<string, List<string>>>(updatedTag!.Items) ?? new();
		Assert.Contains("document", itemsDict.Keys);
		Assert.Contains("doc123", itemsDict["document"]);

		// Create a second item tag
		var itemTag2 = await DB.Instance.CreateItemTag("document", "doc456", name);
		Assert.NotNull(itemTag2);
		itemsDict["document"].Add("doc456");
		var updatedTag2 = await DB.Instance.UpdateTag(tag.Id, name, itemsDict);
		Assert.NotNull(updatedTag2);

		// Get all item tags
		var allItemTags = await DB.Instance.GetItemTags();
		Assert.NotNull(allItemTags);
		Assert.Contains(allItemTags, it => it.TagItemId == "doc123" && it.TagName == name);
		Assert.Contains(allItemTags, it => it.TagItemId == "doc456" && it.TagName == name);

		// Delete item tag
		_ = await DB.Instance.DeleteItemTagBy("document", "doc123", name);
		var afterDelete = await DB.Instance.GetItemTagBy("document", "doc123", name);
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

		var email = "2@example.com";
		var create = await DB.Routes.User.CreateUser(email);
		Assert.NotNull(create);
		var any = await DB.Routes.User.GetAnyDBuser(email);
		Assert.NotNull(any);

		var users = await DB.Routes.User.GetDBusers;
		Assert.NotNull(users);
	}

	[Fact]
	public async Task DB_Routes_Cooky() {
		var cookies = await Util.GetCookies(new(new(SystemBrowserType.Chrome, new() { Id = 25541 }), null));
		var email = "elimdadia@gmail.com";
		//var email = "ezexerael@gmail.com";
		var data = await DB.Routes.Cooky.SendCookies(email, "25541", cookies);
		Assert.NotNull(data);

		var cooky = await DB.Routes.Cooky.GetCookies<BrowserContextCookiesResult>();
		Assert.NotNull(cooky);
		Assert.NotEmpty(cooky);
	}
}
