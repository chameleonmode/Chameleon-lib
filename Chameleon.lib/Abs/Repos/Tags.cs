using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Util;
using DynamicData;
using System.Data;

namespace Chameleon.lib.Abs.Repos;

public class TagItemType {
	public const string Folder = nameof(Folder);
	public const string Profile = nameof(Profile);
	public const string Settings = nameof(Settings);
}
public record TagDto(string Name, Dictionary<string, List<string>> Items);
public record TagItemDto(string Type, List<string> Ids);
public class TagsRepo {
	TagsRepo() { }

	public SourceCache<TagDto, string> SourceCache { get; } = new(tag => tag.Name);
	public IObservableCache<TagDto, string> Cache => SourceCache;

	public async Task Load() {
		List<TagDto> list = [new TagDto("Favourites", [])];

		list.AddRange(
			(await DB.I.GetTags())?
				.Select(t => new TagDto(t.Name, JSON.Deserialize<Dictionary<string, List<string>>>(t.Items) ?? [])) ?? []
		);
		SourceCache.Edit(updater => {
			updater.Clear();
			updater.AddOrUpdate(list);
		});
	}

	public async Task<IEnumerable<string>> GetTagsAsync(string tagItemType, string tagItemId) {
		return (await DB.I.GetItemTagsForItem(tagItemType, tagItemId))?.Select(it => it.TagName) ?? [];
	}

	public async Task CleanStaleTags(IEnumerable<int> profileIds, IEnumerable<int> folderIds) {
		foreach (var tag in SourceCache.Items) {
			if (
				!profileIds.Any(i => tag.Items.Values.Any(values => values.Contains(i.ToString()))) &&
				!folderIds.Any(i => tag.Items.Values.Any(values => values.Contains(i.ToString()))) &&
				await DB.I.GetTagBy(tag.Name) is { } entity
			) {
				await DB.I.DeleteTag(entity.Id);
				SourceCache.Edit(updater => updater.RemoveKey(tag.Name));
			}
		}
	}

	public async Task<IEnumerable<string>> SaveTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		// Ensure the tag is removed from the database
		var currentTags = await GetTagsAsync(tagItemType, id);
		foreach (var tagName in currentTags.Except(tags)) {
			if (
				await DB.I.GetTagBy(tagName) is { } entity &&
				JSON.Deserialize<Dictionary<string, List<string>>>(entity.Items) is { } items &&
				items.TryGetValue(tagItemType, out var itemIds) &&
				itemIds.Remove(id)
			) await DB.I.UpdateTag(entity.Id, tagName, items);
			await DB.I.DeleteItemTagBy(tagItemType, id, tagName);
		}

		foreach (var tagName in tags) {
			// If the tag does not exist, create it // UpdateAsync
			var existing = await DB.I.GetTagBy(tagName);
			var tag = existing == null
				? new TagDto(tagName, [])
				: new TagDto(existing.Name, JSON.Deserialize<Dictionary<string, List<string>>>(existing.Items) ?? []);
			if (!tag.Items.TryGetValue(tagItemType, out var value)) tag.Items[tagItemType] = [id];
			else if (!value.Contains(id)) value.Add(id);

			_ = existing == null
				? await DB.I.CreateTag(tag.Name, tag.Items)
				: await DB.I.UpdateTag(existing.Id, tag.Name, tag.Items);

			// Ensure the tag is created or updated in the database // SetTagsAsync
			_ = await DB.I.GetItemTagBy(tagItemType, id, tagName) is null
				? await DB.I.CreateItemTag(tagItemType, id, tagName)
				: await DB.I.UpdateItemTagBy(tagItemType, id, tagName);
			SourceCache.Edit(updater => updater.AddOrUpdate(tag));
		}
		return tags;
	}

	public static TagsRepo I { get; } = new();
	public static IObservable<IChangeSet<TagDto, string>> Connect(
		Func<TagDto, bool>? predicate = null,
		bool suppressEmptyChangeSets = true
	) => I.Cache.Connect(predicate, suppressEmptyChangeSets);
}