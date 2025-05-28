using System.Text.Json.Serialization;
using Chameleon.lib.Common.Models.Interfaces;

namespace Chameleon.lib.Common.Models.Dto;
public class Dto : IHasid {
	public int id { get; set; }
	[JsonIgnore] public string ID => id.ToString();
	public string? title { get; set; }

	public string? Tags { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class UPFolderDto : Dto {
  public bool isFavorite { get; set; }
  public int profilesCount { get; set; }
  public long? creatorUserId { get; set; }
}