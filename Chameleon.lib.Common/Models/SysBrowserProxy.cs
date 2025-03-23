using System.Net;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Util;

namespace Chameleon.lib.Common.Models;

public class SysBrowserProxy {
	public string? HostForRequest => Host?.Contains("proxy.chameleonmode.com") == true ?
		"proxy.packetstream.io" 
		: Host;
	public string? Server => CanUse ? $"{HostForRequest}:{Port}" : null;
	public string? ServerForRequest => CanUse ? $"http://{Server}" : null;
	public WebProxy? WebProxy => CanUse ? new WebProxy(Server) {
		Credentials = new NetworkCredential(UserName, Password)
	} : null;

	public bool CanUse => Host.IsNot() && Port > 0;
	public bool HasLogin => UserName.IsNot() && Password.IsNot();

	private string? _host;
	private int _port = 80;
	private string? _userName;
	private string? _password;

	public string? Host {
		get => _host;
		set => _host = value?.Trim();
	}

	public string? UserName {
		get => _userName;
		set => _userName = value?.Trim();
	}

	public string? Password {
		get => _password;
		set => _password = value?.Trim();
	}

	public int Port {
		get => _port;
		set {
			if (value is < 0 or > 65535) {
				value = 0;
			}
			_port = value;
		}
	}
}