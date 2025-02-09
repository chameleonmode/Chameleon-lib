using Chameleon.lib.Common.Models.Dto;
using DynamicData;
using System.Reactive.Linq;

namespace Chameleon.lib.Api.Repos;
public class TagsRepo {

	private static readonly Dictionary<string, TagDto> allTags = [];

	private static readonly SourceCache<TagDto, string> cache = new(tag => tag.Name);

	public static IObservableCache<TagDto, string> Cache => cache;

	public static IObservable<IChangeSet<TagDto, string>> Connect(Func<TagDto, bool>? predicate = null, bool suppressEmptyChangeSets = true)
	=> Cache.Connect(predicate, suppressEmptyChangeSets);

	public Task<TagDto?> FindTagAsync(string tagName) {
		allTags.TryGetValue(tagName, out var tag);
		return Task.FromResult(tag);
	}

	public Task<IEnumerable<TagDto>> SearchTagAsync(string tagName) {
		var tags = allTags.Keys.Where(key => key.Contains(tagName)).Select(key => allTags[key]);
		return Task.FromResult(tags);
	}

	public Task<IEnumerable<string>> SetTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		IoC.SetValue(tags, $"{tagItemType}-{id}");
		var allTags = IoC.GetValue<List<string>>("tags") ?? [];

		if(allTags.Any(x => x != $"{tagItemType}-{id}")) {
			allTags.Add($"{tagItemType}-{id}");
		}
		return Task.FromResult(tags);
	}

	public Task<IEnumerable<string>> GetTagsAsync(string tagItemType, string id) {
		var tags = IoC.GetValue<IEnumerable<string>>($"{tagItemType}-{id}");
		return Task.FromResult(tags ?? []);
	}
	public Task<TagDto> SaveAsync(TagDto tag) {

		if(allTags.TryGetValue(tag.Name, out var existingTag)) {
			existingTag = existingTag with { Items = tag.Items };
			return Task.FromResult(existingTag);
		} else
			allTags.Add(tag.Name, tag);

		cache.Edit(updater => updater.AddOrUpdate(tag));
		return Task.FromResult(tag);
	}
	public async Task<TagDto> SaveAsync(string tagName, TagItemDto tagItem) {
		var tag = await FindTagAsync(tagName) ?? new TagDto(tagName, []);
		if (tag.Items.TryGetValue(tagItem.Type, out var ids)) {
			tag.Items[tagItem.Type] = ids;
		} else {
			tag.Items.Add(tagItem.Type, tagItem.Ids);
		}
		await SaveAsync(tag);
		return tag;
	}

	public async Task<TagDto> UpdateAsync(string tagName, string tagItemType, string id) {
		var tag = await FindTagAsync(tagName) ?? new TagDto(tagName, []);
		if (tag.Items.TryGetValue(tagItemType, out var ids)) {
			if(!ids.Any(i => i == id)) {
				ids.Add(id);
			}
		} else {
			tag.Items.Add(tagItemType, [id]);
		}
		await SaveAsync(tag);
		return tag;
	}

	public async Task<IEnumerable<string>> SaveTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		var currentTags = await GetTagsAsync(tagItemType, id);
		var removedTags = currentTags.Except(tags);

		await RemoveItemFromTagsAsync(tagItemType, id,removedTags);

		foreach(var tag in tags) {
			await UpdateAsync(tag, tagItemType, id);
		}
		await SetTagsAsync(tagItemType, id, tags);

		return tags;
	}

	public async Task RemoveItemFromTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		foreach (var item in tags) {
			var tag = await FindTagAsync(item);
			if(tag is not null && tag.Items.TryGetValue(tagItemType, out var ids) && ids.Contains(id)) {
				ids.Remove(id);
				tag.Items[tagItemType] = ids;
				await SaveAsync(tag);
			}
		}
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
