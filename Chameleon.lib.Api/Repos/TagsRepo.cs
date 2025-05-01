using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Common.Models.Dto;
using Chameleon.lib.Const;
using DynamicData;
using System.Data;

namespace Chameleon.lib.Api.Repos;
public class TagsRepo {
	TagsRepo() { }

	readonly SourceCache<TagDto, string> cache = new(tag => tag.Name);
	public IObservableCache<TagDto, string> Cache => cache;

	public async Task Load() {
		List<TagDto> list = [new TagDto("Favourites", [])];
		
		list.AddRange(
			(await DB.Instance.GetTags())?
				.Select(t => new TagDto(t.Name, JS.Deserialize<Dictionary<string, List<string>>>(t.Items) ?? [])) ?? []
		);
		cache.Edit(updater => {
			updater.Clear();
			updater.AddOrUpdate(list);
		});
	}

	public async Task<TagDto?> FindTagAsync(string tagName) {
		var tag = await DB.Instance.GetTagBy(tagName);
		return tag == null ? null
			: new TagDto(tag.Name, JS.Deserialize<Dictionary<string, List<string>>>(tag.Items) ?? []);
	}

	public async Task<IEnumerable<string>> SetTagsAsync(string tagItemType, string tagItemId, IEnumerable<string> tags) {
		foreach (var tagName in tags) {
			_ = await DB.Instance.GetItemTagBy(tagItemType, tagItemId, tagName) is null
				? await DB.Instance.CreateItemTag(tagItemType, tagItemId, tagName)
				: await DB.Instance.UpdateItemTagBy(tagItemType, tagItemId, tagName);
		}
		return tags;
	}

	public async Task<IEnumerable<string>> GetTagsAsync(string tagItemType, string tagItemId) {
		return (await DB.Instance.GetItemTagsForItem(tagItemType, tagItemId))?.Select(it => it.TagName) ?? [];
	}

	public async Task<TagDto> SaveAsync(TagDto tag) {
		var existing = await DB.Instance.GetTagBy(tag.Name);
		var current = existing == null
			? await DB.Instance.CreateTag(tag.Name, tag.Items)
			: await DB.Instance.UpdateTag(existing.Id, tag.Name, tag.Items);
		cache.Edit(updater => updater.AddOrUpdate(tag));
		return cache.Lookup(tag.Name).Value;
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

		return await SaveAsync(tag);
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

		foreach (var tagName in removedTags) {
			await RemoveTagFromItemAsync(tagItemType, id, tagName);
		}

		foreach (var tag in tags) {
			_ = await UpdateAsync(tag, tagItemType, id);
		}

		_ = await SetTagsAsync(tagItemType, id, tags);

		foreach (var tagName in removedTags) {
			var tag = await FindTagAsync(tagName);
			if (tag is not null && tag.Items.All(x => x.Value.Count == 0)) {
				var tagEntity = await DB.Instance.GetTagBy(tag.Name);
				await DB.Instance.DeleteTag(tagEntity!.Id);
				cache.Edit(updater => updater.RemoveKey(tagEntity.Name));
			}
		}

		return tags;
	}

	private static async Task RemoveTagFromItemAsync(string tagItemType, string tagItemId, string tagName) {
		_ = await DB.Instance.DeleteItemTagBy(tagItemType, tagItemId, tagName);
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

	public static TagsRepo Instance { get; } = new();
	public static IObservable<IChangeSet<TagDto, string>> Connect(
		Func<TagDto, bool>? predicate = null,
		bool suppressEmptyChangeSets = true
	) => Instance.Cache.Connect(predicate, suppressEmptyChangeSets);
}
