using Chameleon.lib.Common.Models.Dto;

namespace Chameleon.lib.Api.Repos;
public class UserAssistantRepo : ApiBase<AssistDto> {
	private UserAssistantRepo() : base(Consts.Api.Endpoints.AssistantUser) { }

	//public static Task<RootResult> UpdateProfilePermission(AssisProfilePermissionDto permissionDto)
	//{
	//	ArgumentNullException.ThrowIfNull(permissionDto);
	//	return permissionDto.IsGranted
	//		? Instance.Post("InsertProfilePermission", permissionDto)
	//		: HttpApiClient.Instance.Delete<RootResult>($"DeleteProfilePermission?profileAssistantId={permissionDto.ProfileAssistantId}&profilePermissionId={permissionDto.id}");
	//}

	//public static Task<AssisProfilePermissionDto[]> GetAllProfilePermissions(long assistantId, int profileId)
	//{
	//	return Instance.Get<AssisProfilePermissionDto[]>($"GetAllProfilePermissions?assistantId={assistantId}&profileId={profileId}");
	//}

	//public void InsertProfilePermission(IAssistantProfilePermission assistantProfilePermission)
	//{
	//	_apiClient.Post(GetEndpointUrl("InsertProfilePermission"), assistantProfilePermission);
	//}

	//public void DeleteProfilePermission(long profileAssistantId, int profilePermissionId)
	//{
	//	_apiClient.Delete(GetEndpointUrl($"DeleteProfilePermission?profileAssistantId={profileAssistantId}&profilePermissionId={profilePermissionId}"));
	//}
	//public void ShareUserProfile(CreateAssistantProfileDto input)
	//{
	//	_apiClient.Post(GetEndpointUrl("ShareUserProfile"), input);
	//}

	public static Task<RootResult> DeleteAssistantProfile(long assistantId, int profileId) =>
		HttpApiClient.Instance.Delete<RootResult>($"{Instance.Endpoint}DeleteAssistantProfile?assistantId={assistantId}&profileId={profileId}");
	
	public static Task<AssisProfileDto[]> GetAllAssistantProfilesById(long assistantId) =>
		 Instance.Get<AssisProfileDto[]>($"GetAllAssistantProfilesById?assistantId={assistantId}");

	public static async Task<RootResult?> AddProfiles(
		long assistantId, IEnumerable<int> profileIds, IEnumerable<int>? profilePermissions = null
		) =>
   profileIds.Any() ?	await Instance.Post<RootResult>("AddProfiles", new {
			Id = assistantId,
			ProfileIds = profileIds,
			ProfilePermissionIds = profilePermissions ?? []
		}) : null;
	
	public static Task SetCanCreateProfiles(long assistantId, bool canCreateProfiles) => 
		Instance.Post($"SetCanCreateProfiles?assistantId={assistantId}&canCreateProfiles={canCreateProfiles}");

	public static UserAssistantRepo Instance { get; } = new UserAssistantRepo();
}

public class ShareFoldersRepo : ApiBase<AssisShareFolderDto> {
	private ShareFoldersRepo() : base(Consts.Api.Endpoints.ShareFolders) { }

	//public string[] GetAllProfilePermissionNames(long userId, int profileId, int? folderId)
	//{
	//	return _apiClient.Get<string[]>(GetEndpointUrl($"GetAllProfilePermissionNames?userId={userId}&profileId={profileId}&folderId={folderId}"));
	//}

	//public static Task AddPermission(int userFolderId, int permissionId)
	//{
	//	return Instance.Post("AddPermission", new
	//	{
	//		UserFolderId = userFolderId,
	//		PermissionId = permissionId
	//	});
	//}

	//public static Task DeletePermission(int userFolderId, int permissionId)
	//{
	//	return HttpApiClient.Instance.Delete<RootResult>($"DeletePermission?userFolderId={userFolderId}&permissionId={permissionId}");
	//}

	record GetAllResult(int TotalCount, AssisShareFolderDto[] Items);
	public static async Task<AssisShareFolderDto[]> GetAll(long userId)
	{
		var response = await HttpApiClient.Instance.Get<GetAllResult>($"{Instance.Endpoint}GetAll?UserId={userId}");
		return response.Items;
	}

	public static async Task<AssisShareFolderDto[]> Share(
		long assistantId, IEnumerable<int> folderIds, IEnumerable<int>? folderpermissionIds = null
	) {
		return folderIds.Any() ? await Instance.Post<AssisShareFolderDto[]>("Share", new {
			UserId = assistantId,
			FolderIds = folderIds,
			PermissionIds = folderpermissionIds ?? []
		}) : [];
		// List<AssisShareFolderDto> sharedFolders = [];
		// foreach (var folderId in folderIds) { //ToDo: fix server side issue ??

		// 	var folders = await Instance.Post<AssisShareFolderDto[]>("Share", new {
		// 		UserId = assistantId,
		// 		FolderIds = new List<int>([folderId]),
		// 		PermissionIds = folderpermissionIds
		// 	});
		// 	sharedFolders.AddRange(folders);
		// }
		// return [.. sharedFolders];
	}

	public static ShareFoldersRepo Instance { get; } = new ShareFoldersRepo();
}
