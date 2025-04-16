using Chameleon.lib.Common.Constants;
using Chameleon.lib.Util;

namespace Chameleon.lib.WebBrowser.Models;

public record SysBrowserOpenOptions(Enums.SystemBrowserType BrowserType, BrowserProfile Profile);
public class BrowserProfile {
	public int Id { get; set; }
	public int Port { get; set; } = TcpUtil.NextFreePort(9613);
	public BrowserProxy Proxy { get; set; } = new();
	public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();

	public string[] DefaultHomePageSettings { get; init; } =
		IoC.GetJsonValue<string[]>(nameof(DefaultHomePageSettings))
			.Let(urls => urls != null && urls.Length > 0 ? new[] { urls[new Random().Next(urls.Length)] } : ["example.com"]);

	public string StartUrl { get; init; } =
		IoC.GetJsonValue<string[]>(nameof(DefaultHomePageSettings))
			.Let(urls => urls != null && urls.Length > 0 ? urls[new Random().Next(urls.Length)] : "example.com")
			.Let(randomUrl => Uri.TryCreate(randomUrl, UriKind.Absolute, out var uriResult)
				&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
					? uriResult.AbsoluteUri
					: "http://" + randomUrl);
}

public record class BrowserRecord(string Name, string Path) {
	public override string ToString() {
		return Name ?? Path;
	}
}