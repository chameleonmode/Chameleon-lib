using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.Common.Models;

public class ProxySettingsModel {
	public string HostForRequest => Host.Contains(Consts.Http.ChameleonModeHost) ? Consts.Http.PacketStreamHost : Host;
	public string Server => CanUse ? $"{HostForRequest}:{Port}" : string.Empty;
	public string ServerForRequest => CanUse ? $"http://{Server}" : string.Empty;

	public bool CanUse => Host.Is() && Port > 0;
	public bool HasLogin => UserName.Is() && Password.Is();

	private string _host = string.Empty;
	private int _port = 80;
	private string _userName = string.Empty;
	private string _password = string.Empty;

	public string? Host {
		get => _host;
		set => _host = value?.Trim() ?? string.Empty;
	}

	public string? UserName {
		get => _userName;
		set => _userName = value?.Trim() ?? string.Empty;
	}

	public string? Password {
		get => _password;
		set => _password = value?.Trim() ?? string.Empty;
	}

	public int Port {
		get => _port;
		set {
			if (value is < 0 or >= 65535) {
				value = 0;
			}
			_port = value;
		}
	}
}