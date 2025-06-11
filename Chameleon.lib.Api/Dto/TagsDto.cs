namespace Chameleon.lib.Api.Dto;

public class TagItemType {
	public const string Folder = nameof(Folder);
	public const string Profile = nameof(Profile);
	public const string Settings = nameof(Settings);
}
public record TagDto(string Name, Dictionary<string,List<string>> Items);
public record TagItemDto(string Type, List<string> Ids);
