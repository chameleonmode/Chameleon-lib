using Chameleon.lib.Common.Constants;
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

	public static Task<RootResult> AddProfiles(long assistantId, IList<int> profileIds, IList<int> profilePermissions) =>
		Instance.Post<RootResult>("AddProfiles", new {
			Id = assistantId,
			ProfileIds = profileIds,
			ProfilePermissionIds = profilePermissions
		});
	
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

	public static Task<AssisShareFolderDto[]> GetAll(long userId)
	{
		return HttpApiClient.Instance.Get<AssisShareFolderDto[]>($"{Instance.Endpoint}GetAll", new
		{
			MaxResultCount = int.MaxValue,
			UserId = userId,
		});
	}

	public static Task<AssisShareFolderDto[]> Share(long assistantId, IList<int> folderIds, IList<int> folderpermissionIds)
	{
		return Instance.Post<AssisShareFolderDto[]>("Share", new
		{
			UserId = assistantId,
			FolderIds = folderIds,
			PermissionIds = folderpermissionIds
		});
	}

	public static ShareFoldersRepo Instance { get; } = new ShareFoldersRepo();
}
