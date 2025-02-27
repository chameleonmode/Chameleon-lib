using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Storage;
using DynamicData;
using System.Data;
using System.Text.Json;

namespace Chameleon.lib.Api.Repos;
public class TagsRepo {

	private static readonly SourceCache<TagDto, string> cache = new(tag => tag.Name);

	public static IObservableCache<TagDto, string> Cache => cache;

	private readonly SqliteStorageService storage = SqliteStorageService.Instance;

	private bool isInitialized = false;

	private TagsRepo() {
		Initialize();
	}
	public void Initialize() {
		if (!isInitialized) {
			storage.CreateTable(
					tableName: "Tags",
					columns: new Dictionary<string, string>
					{
															{ "Name",  "TEXT PRIMARY KEY" },
															{ "Items", "TEXT"             }
					}
					);

			storage.CreateTable(
					tableName: "ItemTags",
					columns: new Dictionary<string, string>
					{
										{ "TagItemType", "TEXT" },
										{ "Id",          "TEXT" },
										{ "TagName",     "TEXT" }
					}
			);

			LoadCacheFromDatabase();

			isInitialized = true;
		}
	}

	public static IObservable<IChangeSet<TagDto, string>> Connect(
						Func<TagDto, bool>? predicate = null,
						bool suppressEmptyChangeSets = true)
						=> Cache.Connect(predicate, suppressEmptyChangeSets);

	public Task<TagDto?> FindTagAsync(string tagName) {
		if (string.IsNullOrEmpty(tagName))
			return Task.FromResult<TagDto?>(null);

		var dataTable = storage.Query(
				"SELECT Items FROM Tags WHERE Name=@name",
				new Dictionary<string, object> { { "name", tagName } }
		);

		if (dataTable.Rows.Count == 0)
			return Task.FromResult<TagDto?>(null);

		var itemsJson = dataTable.Rows[0]["Items"]?.ToString() ?? "{}";
		var itemsDict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(itemsJson)
									 ?? [];

		var tag = new TagDto(tagName, itemsDict);
		return Task.FromResult<TagDto?>(tag);
	}

	public Task<IEnumerable<TagDto>> SearchTagAsync(string tagName) {
		if (string.IsNullOrEmpty(tagName))
			return Task.FromResult<IEnumerable<TagDto>>([]);

		var dataTable = storage.Query(
				"SELECT Name, Items FROM Tags WHERE Name LIKE @tagName",
				new Dictionary<string, object> { { "tagName", $"%{tagName}%" } }
		);

		var results = new List<TagDto>();
		foreach (DataRow row in dataTable.Rows) {
			var name = row["Name"]?.ToString() ?? string.Empty;
			var itemsJson = row["Items"]?.ToString() ?? "{}";
			var itemsDict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(itemsJson)
										 ?? [];
			results.Add(new TagDto(name, itemsDict));
		}

		return Task.FromResult<IEnumerable<TagDto>>(results);
	}

	public Task<IEnumerable<string>> SetTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {

		_ = storage.Delete(
				"ItemTags",
				"TagItemType=@type AND Id=@id",
				new Dictionary<string, object>
				{
										{ "type", tagItemType },
										{ "id",   id           }
				}
		);

		foreach (var tag in tags) {
			var values = new Dictionary<string, object>
			{
										{ "TagItemType", tagItemType },
										{ "Id",          id           },
										{ "TagName",     tag            }
								};
			_ = storage.Insert("ItemTags", values);
		}

		return Task.FromResult(tags);
	}

	public Task<IEnumerable<string>> GetTagsAsync(string tagItemType, string id) {
		var dataTable = storage.Query(
				"SELECT TagName FROM ItemTags WHERE TagItemType=@type AND Id=@id",
				new Dictionary<string, object>
				{
										{ "type", tagItemType },
										{ "id",   id           }
				}
		);

		var results = new List<string>();
		foreach (DataRow row in dataTable.Rows) {
			if (row["TagName"] != null) {
				results.Add(row["TagName"].ToString()!);
			}
		}

		return Task.FromResult<IEnumerable<string>>(results);
	}

	public async Task<TagDto> SaveAsync(TagDto tag) {

		var existingTag = await FindTagAsync(tag.Name);

		var itemsJson = JsonSerializer.Serialize(tag.Items);

		_ = existingTag == null
			? storage.Insert(
					"Tags",
					new Dictionary<string, object>
					{
												{ "Name",  tag.Name  },
												{ "Items", itemsJson }
					}
			)
			: storage.Update(
					"Tags",
					new Dictionary<string, object>
					{
												{ "Items", itemsJson }
					},
					"Name=@name",
					new Dictionary<string, object> { { "name", tag.Name } }
			);

		cache.Edit(updater => updater.AddOrUpdate(tag));

		return tag;
	}

	public async Task<TagDto> SaveAsync(string tagName, TagItemDto tagItem) {

		var tag = await FindTagAsync(tagName) ?? new TagDto(tagName, []);

		if (!tag.Items.ContainsKey(tagItem.Type)) {
			tag.Items[tagItem.Type] = [.. tagItem.Ids];
		} else {
			foreach (var newId in tagItem.Ids) {
				if (!tag.Items[tagItem.Type].Contains(newId))
					tag.Items[tagItem.Type].Add(newId);
			}
		}

		_ = await SaveAsync(tag);
		return tag;
	}

	public async Task<TagDto> UpdateAsync(string tagName, string tagItemType, string id) {
		var tag = await FindTagAsync(tagName) ?? new TagDto(tagName, []);

		if (!tag.Items.ContainsKey(tagItemType)) {
			tag.Items[tagItemType] = [id];
		} else {
			if (!tag.Items[tagItemType].Contains(id)) {
				tag.Items[tagItemType].Add(id);
			}
		}

		_ = await SaveAsync(tag);
		return tag;
	}

	public async Task<IEnumerable<string>> SaveTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		var currentTags = await GetTagsAsync(tagItemType, id);
		var removedTags = currentTags.Except(tags);

		await RemoveItemFromTagsAsync(tagItemType, id, removedTags);

		foreach (var tag in tags) {
			_ = await UpdateAsync(tag, tagItemType, id);
		}

		_ = await SetTagsAsync(tagItemType, id, tags);

		return tags;
	}

	public async Task RemoveItemFromTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		foreach (var tagName in tags) {
			var tag = await FindTagAsync(tagName);
			if (tag is not null && tag.Items.TryGetValue(tagItemType, out var ids)) {
				if (ids.Remove(id))
					_ = await SaveAsync(tag);
			}
		}
	}

	private void LoadCacheFromDatabase() {
		var dt = storage.Query("SELECT Name, Items FROM Tags");
		var list = new List<TagDto>();

		foreach (DataRow row in dt.Rows) {
			var name = row["Name"]?.ToString() ?? string.Empty;
			var itemsJson = row["Items"]?.ToString() ?? "{}";
			var itemsDict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(itemsJson)
										 ?? [];
			list.Add(new TagDto(name, itemsDict));
		}

		cache.Edit(updater => updater.AddOrUpdate(list));
	}

	private static TagsRepo? _instance;
	private static readonly object _lock = new();
	public static TagsRepo Instance {
		get {
			lock (_lock) {
				return _instance ??= new TagsRepo();
			}
		}
	}

}
