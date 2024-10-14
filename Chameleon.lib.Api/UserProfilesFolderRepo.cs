using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Interfaces;

namespace Chameleon.lib.Api;
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class UserProfileFolderDto : IHasid {
	public int id { get; set; }
	public string? title { get; set; }
	public bool isFavorite { get; set; }
	public int profilesCount { get; set; }
	public long? creatorUserId { get; set; }
}

public class UserProfilesFolderRepo : ApiBase<UserProfileFolderDto> {
	private UserProfilesFolderRepo() : base(Consts.Api.FolderEndpoint) { }
	public static Task<UserProfileFolderDto> CreateFolder(string title)
	{
		if (string.IsNullOrEmpty(title)) {
			//title = $"New Folder {folders.Count + 1}";
		}
		return Instance.Create(new {
			Title = title
		});
	}

	public static UserProfilesFolderRepo Instance { get; } = new UserProfilesFolderRepo();
}