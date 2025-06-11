using System.Text.Json.Serialization;

namespace Chameleon.lib.Api.Dto;
public interface IDto {
	int id { get; set; }
	public string? title { get; set; }
}
public class Dto : IDto {
	public int id { get; set; }
	[JsonIgnore] public string ID => id.ToString();
	public string? title { get; set; }

	public string? Tags { get; set; }
}

public class UPFolderDto : Dto {
  public bool isFavorite { get; set; }
  public int profilesCount { get; set; }
  public long? creatorUserId { get; set; }
}