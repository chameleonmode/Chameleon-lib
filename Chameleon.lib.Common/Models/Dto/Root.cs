namespace Chameleon.lib.Common.Models.Dto;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class RootResponse<T> {
	public T? result { get; set; }
	public object? targetUrl { get; set; }
	public bool success { get; set; }
	public Error? error { get; set; }
	public bool unAuthorizedRequest { get; set; }
	public bool __abp { get; set; }
}

public class RootResult : RootResponse<object> {
}

public class Result<T> {
	public int totalCount { get; set; }
	public T[]? items { get; set; }
}

public class Error
{
	public int code { get; set; }
	public string? message { get; set; }
	public object? details { get; set; }
	public object? validationErrors { get; set; }
}