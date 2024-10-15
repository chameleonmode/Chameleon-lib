namespace Chameleon.lib.Common.Models.Dto;
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class UPFolderDto : Interfaces.Dto {
	public bool isFavorite { get; set; }
	public int profilesCount { get; set; }
	public long? creatorUserId { get; set; }
}
