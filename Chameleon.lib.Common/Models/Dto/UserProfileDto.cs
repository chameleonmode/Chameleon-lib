using Chameleon.lib.Common.Models.Interfaces;

namespace Chameleon.lib.Common.Models.Dto;
//public string YoutubeApiKey { get; set; }
//public string YoutubeClientId { get; set; }
//public string YoutubeClientSecret { get; set; }
//public WordPressSettingsDto WordPressSettings { get; set; }
//public class WebBrowserDto {
//	public bool WebRTC { get; set; }
//	public bool WebGL { get; set; }
//	public bool Tracking { get; set; }
//	public bool Flash { get; set; }
//	public decimal Canvas { get; set; }
//	public int? UserAgentId { get; set; }
//}
//public record ProxyDto(string Host, int Port, string? UserName, string? Password);
//public record UserProfileDto(int Id, string Title, string Notes, bool IsFavourite, int? FolderId, long? CreatorUserId, double? LimitCache);
//public object? youtubeApiKey { get; set; }
//public object? youtubeClientId { get; set; }
//public object? youtubeClientSecret { get; set; }
//public object? wordPressSettings { get; set; }
//public Webbrowser? webBrowser { get; set; }
//[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
//public class Webbrowser {
//	public bool webRTC { get; set; }
//	public bool webGL { get; set; }
//	public bool tracking { get; set; }
//	public bool flash { get; set; }
//	public float canvas { get; set; }
//	public int? userAgentId { get; set; }
//}
//public object? businesses { get; set; }
//public object? Logins { get; set; }
//public object? Persons { get; set; }
//public object? Addresses { get; set; }
//public float limitCache { get; set; }
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class UserProfileDto : Interfaces.Dto {
	public int creatorUserId { get; set; }
	public int? folderId { get; set; }
	public bool isFavourite { get; set; }
	public string? notes { get; set; }
	public int proxyId { get; set; }
	public Proxy? proxy { get; set; }

}
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class Proxy {
	public string? host { get; set; }
	public int port { get; set; }
	public string? userName { get; set; }
	public string? password { get; set; }
}