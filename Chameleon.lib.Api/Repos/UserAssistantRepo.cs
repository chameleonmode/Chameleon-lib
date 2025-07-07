using Chameleon.lib.Api.Dto;
using DynamicData;

namespace Chameleon.lib.Api.Repos;

public class UserAssistantRepo : ApiBase<AssistDto> {
	private UserAssistantRepo() : base(Consts.Api.Endpoints.AssistantUser) { }
	public static Task<RootResult> DeleteAssistantProfile(long assistantId, int profileId) =>
		HttpApiClient.Instance.Delete<RootResult>($"{Instance.Endpoint}DeleteAssistantProfile?assistantId={assistantId}&profileId={profileId}");

	public static Task<AssisProfileDto[]> GetAllAssistantProfilesById(long assistantId) =>
		 Instance.Get<AssisProfileDto[]>($"GetAllAssistantProfilesById?assistantId={assistantId}");

	public static async Task AddProfiles(long assistantId, IEnumerable<int> profileIds, IEnumerable<int>? profilePermissions = null) {
		if (!profileIds.Any()) return;

		var result = await Instance.Post<RootResult>("AddProfiles", new {
			Id = assistantId,
			ProfileIds = profileIds,
			ProfilePermissionIds = profilePermissions ?? []
		}) ?? throw new Exception("Failed to add profiles");
		if (!result.success) throw new Exception(result.error?.message ?? "Failed to add profiles");
	}

	public static Task SetCanCreateProfiles(long assistantId, bool canCreateProfiles) =>
		Instance.Post($"SetCanCreateProfiles?assistantId={assistantId}&canCreateProfiles={canCreateProfiles}");

	public static UserAssistantRepo Instance { get; } = new UserAssistantRepo();
}

public class ShareFoldersRepo : ApiBase<AssisShareFolderDto> {
	private ShareFoldersRepo() : base(Consts.Api.Endpoints.ShareFolders) { }
	record GetAllResult(int TotalCount, AssisShareFolderDto[] Items);
	public static async Task<AssisShareFolderDto[]> GetAll(long userId) {
		var response = await HttpApiClient.Instance.Get<GetAllResult>($"{Instance.Endpoint}GetAll?UserId={userId}");
		return response.Items;
	}

	public static async Task<AssisShareFolderDto[]> Share(
		 long assistantId, IEnumerable<int> folderIds, IEnumerable<int>? folderpermissionIds = null
	) {
		List<AssisShareFolderDto> shared = [];
		foreach (var folderId in folderIds) { //TODO: fix server side issue ?? no keep this fix here for now

			var folders = await Instance.Post<AssisShareFolderDto[]>("Share", new {
				UserId = assistantId,
				FolderIds = new List<int>([folderId]),
				PermissionIds = folderpermissionIds
			});
			shared.AddRange(folders);
		}
		return [.. shared];
	}

	public static ShareFoldersRepo Instance { get; } = new ShareFoldersRepo();
}
