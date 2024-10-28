namespace Chameleon.lib.Common.Models.Dto;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class RootResponse<T> {
	public T? result { get; set; }
	public object? targetUrl { get; set; }
	public bool success { get; set; }
	public object? error { get; set; }
	public bool unAuthorizedRequest { get; set; }
	public bool __abp { get; set; }
}

public class RootResult : RootResponse<object> {
}

public class Result<T> {
	public int totalCount { get; set; }
	public T[]? items { get; set; }
}

