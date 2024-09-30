namespace Chameleon.lib.Common.Models;

public class UserProfileModel {
	private int _id;
	public int Id {
		get => _id;
		set {
			if (value <= 0) {
				return;
			}
			_id = value;
		}
	}

	private ProxySettingsModel _proxy = new();
	public ProxySettingsModel Proxy {
		get => _proxy;
		set => _proxy = value ?? new ProxySettingsModel();
	}
}