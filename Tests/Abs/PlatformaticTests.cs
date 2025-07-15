using System.Diagnostics;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Api.Dto;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Util;

using Microsoft.Playwright;

namespace Tests.Abs;

public class PlatformaticTests : TestSetup {
	public PlatformaticTests() : base(0) { }

	#region DB users and data interactiions
	[Fact]
	public async Task DB_GetDataInteractions() {
		await DB.I.Userz.Load();
		var datas = await DB.I.Interactions.GetDataInteractions();
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_PostDataInteraction() {
		await DB.I.Userz.Load();
		var datas = await DB.I.Interactions.PostDataInteraction(new(
			ReceiverId: "ef61cf83-13e7-486a-ac37-84ec78841b4f",
			DataType: "poop",
			DataPayload: "poop"
		));
		Assert.NotNull(datas);
	}

	[Fact]
	public async Task DB_DeleteDataInteractions() {
		await DB.I.Userz.Load();
		await DB.I.Interactions.DeleteDataInteractions(DB.Routes.Interactions.Types.cooky);
		var data = await DB.I.Interactions.GetDataInteractions(DB.Routes.Interactions.Types.cooky);
		Assert.Empty(data);

		await DB.I.Interactions.DeleteDataInteractions();
		var all = await DB.I.Interactions.GetDataInteractions();
		Assert.Empty(all!);
	}
	#endregion

	[Fact]
	public async Task DB_Tags() {
		await DB.I.Userz.Load();
		// Create a tag
		var name = "tester8";
		var create = await DB.I.CreateTag(name, []);
		Assert.NotNull(create);
		Assert.Equal(name, create.Name);

		// Get the tag by id
		var get = await DB.I.GetTag(create.Id);
		Assert.NotNull(get);
		Assert.Equal(name, get.Name);

		// Get the tag
		var getBy = await DB.I.GetTagBy(name);
		Assert.NotNull(getBy);
		Assert.Equal(getBy.Name, get.Name);

		// Update the tag
		var update = await DB.I.UpdateTag(create.Id, name, new Dictionary<string, List<string>> {
						{ "folder", new List<string> { "id", "id2", "id3" } }
				});
		Assert.NotNull(update);
		Assert.Equal(name, update.Name);

		var tag = new TagDto(update.Name, JSON.Deserialize<Dictionary<string, List<string>>>(update.Items)!);
		Assert.Contains("folder", tag.Items.Keys);
		Assert.Contains("id", tag.Items["folder"]);
		Assert.Contains("id2", tag.Items["folder"]);

		// Verify update
		get = await DB.I.GetTag(create.Id);
		tag = new TagDto(get!.Name, JSON.Deserialize<Dictionary<string, List<string>>>(get.Items)!);
		Assert.Contains("id3", tag.Items["folder"]);

		// List all tags
		var list = (await DB.I.GetTags())?.Select(
				t => new TagDto(t.Name, JSON.Deserialize<Dictionary<string, List<string>>>(t.Items)!)
		);
		Assert.NotNull(list);
		Assert.NotEmpty(list);
		Assert.Contains(list, t => t.Name == name);

		// Search for tags
		var searchResults = await DB.I.SearchTags("te");
		Assert.NotNull(searchResults);
		Assert.Contains(searchResults, t => t.Name == name);

		// Clean up
		await DB.I.DeleteTag(create.Id);
		get = await DB.I.GetTag(create.Id);
		Assert.Null(get);
	}

	[Fact]
	public async Task DB_ItemTags() {
		await DB.I.Userz.Load();

		var name = "testTagingz5";
		// Create a tag first
		var tag = await DB.I.CreateTag(name, []);
		Assert.NotNull(tag);

		// Create an item tag
		var itemTag = await DB.I.CreateItemTag("document", "doc123", name);
		Assert.NotNull(itemTag);
		Assert.Equal("document", itemTag.TagItemType);
		Assert.Equal("doc123", itemTag.TagItemId);
		Assert.Equal(name, itemTag.TagName);

		// Get the item tag
		var getItemTag = await DB.I.GetItemTagBy("document", "doc123", name);
		Assert.NotNull(getItemTag);
		Assert.Equal("document", getItemTag.TagItemType);
		Assert.Equal("doc123", getItemTag.TagItemId);
		Assert.Equal(name, getItemTag.TagName);

		// Get item tags for item
		var itemTags = await DB.I.GetItemTagsForItem("document", "doc123");
		Assert.NotNull(itemTags);
		Assert.NotEmpty(itemTags);
		Assert.Contains(itemTags, it => it.TagName == name);

		// Update the tag's items collection
		var update = await DB.I.UpdateTag(tag.Id, name, new Dictionary<string, List<string>> {
			{ "document", new List<string> { "doc123" } }
		});
		Assert.NotNull(update);
		var updatedTag = await DB.I.GetTag(tag.Id);
		var itemsDict = JSON.Deserialize<Dictionary<string, List<string>>>(updatedTag!.Items) ?? new();
		Assert.Contains("document", itemsDict.Keys);
		Assert.Contains("doc123", itemsDict["document"]);

		// Create a second item tag
		var itemTag2 = await DB.I.CreateItemTag("document", "doc456", name);
		Assert.NotNull(itemTag2);
		itemsDict["document"].Add("doc456");
		var updatedTag2 = await DB.I.UpdateTag(tag.Id, name, itemsDict);
		Assert.NotNull(updatedTag2);

		// Get all item tags
		var allItemTags = await DB.I.GetItemTags();
		Assert.NotNull(allItemTags);
		Assert.Contains(allItemTags, it => it.TagItemId == "doc123" && it.TagName == name);
		Assert.Contains(allItemTags, it => it.TagItemId == "doc456" && it.TagName == name);

		// Delete item tag
		_ = await DB.I.DeleteItemTagBy("document", "doc123", name);
		var afterDelete = await DB.I.GetItemTagBy("document", "doc123", name);
		Assert.Null(afterDelete);

		// Clean up
		await DB.I.DeleteTag(tag.Id);
		var getTag = await DB.I.GetTag(tag.Id);
		Assert.Null(getTag);
	}

	#region Service
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
	public async Task Service_Routes_Promptee() {
		var res = await Service.I.Robo.Genorate(new(
			new(System: "you are helpful", Tone: "roboto", Human: "mr", Audience: "roboto", Background: "domo"), 2, ["popeye", "captain crunch"])
		);
		Debug.WriteLine(JSON.Serialize(res!));
	}
	#endregion

	[Fact]
	public async Task DB_Routes_Cooky() {
		var cookies = await Util.GetCookies(new(new(Chameleon.lib.Browzer.BrowserType.Chrome, new() { Id = 25541 }), null));
		var email = "elimdadia@gmail.com";
		//var email = "ezexerael@gmail.com";
		await DB.I.Cooky.SendCookies(25541, email, cookies ?? throw new InvalidOperationException("Failed to get cookies from browser profile"));
		var cooky = await DB.I.Cooky.GetCookies<BrowserContextCookiesResult>();
		Assert.NotNull(cooky);
		Assert.NotEmpty(cooky);
	}
}
