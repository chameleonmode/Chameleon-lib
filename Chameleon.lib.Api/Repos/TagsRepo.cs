using Chameleon.lib.Common;
using Chameleon.lib.Common.Models.Dto;

namespace Chameleon.lib.Api.Repos;
public class TagsRepo {
	public Task<TagDto?> FindTagAsync(string tagName) {
		var tag = IoC.GetValue<TagDto>(tagName);
		return Task.FromResult(tag);
	}

	public Task<IEnumerable<string>> SetTagsAsync(string tagItemType, string id, IEnumerable<string> tags) {
		IoC.SetValue(tags, $"{tagItemType}-{id}");
		return Task.FromResult(tags);
	}

	public Task<IEnumerable<string>> GetTagsAsync(string tagItemType, string id) {
		var tags = IoC.GetValue<IEnumerable<string>>($"{tagItemType}-{id}");
		return Task.FromResult(tags ?? []);
	}
	public Task<TagDto> SaveAsync(TagDto tag) {

		var exisitingTag = IoC.GetValue<TagDto>(tag.Name) ?? tag;
		exisitingTag = exisitingTag with { Items = tag.Items };
		IoC.SetValue(tag, tag.Name);

		return Task.FromResult(exisitingTag);
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
