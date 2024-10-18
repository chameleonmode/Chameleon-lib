namespace Chameleon.lib.Common.Models.Dto;
public class AssistDto : Interfaces.Dto {
	public string? UserName { get; set; }
	public string? Name { get; set; }
	public string? Surname { get; set; }
	public string? EmailAddress { get; set; }
	public string? Password { get; set; }
	public bool IsActive { get; set; }
	public bool CanCreateProfiles { get; set; }
	public string[] RoleNames { get; set; } = [];
	public IList<int> ProfileIds { get; set; } = [];
	public IList<int> ProfilePermissionIds { get; set; } = [];
	public IList<int> FolderIds { get; set; } = [];
	public IList<int> FolderPermissionIds { get; set; } = [];
}

public class AssisProfileDto : Interfaces.Dto {
	public int ProfileId { get; set; }
	public string? ProfileName { get; set; }
}

public class AssisProfilePermissionDto : Interfaces.Dto {
	public string? PermissionName { get; set; }
	public string? DisplayName { get; set; }
	public bool IsGranted { get; set; }
	public long ProfileAssistantId { get; set; }
}

public class AssisShareFolderDto : Interfaces.Dto {
	public long UserId { get; set; }
	public int FolderId { get; set; }
	public string? FolderName { get; set; }
	public List<AssisShareFolderPermission> FolderPermissions { get; set; } = [];
}
public class AssisShareFolderPermission : Interfaces.Dto {
	public int PermissionId { get; set; }
	public string? PermissionName { get; set; }
	public string? DisplayName { get; set; }
	public bool IsGranted { get; set; }
}
