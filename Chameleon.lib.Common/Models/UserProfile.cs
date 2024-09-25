namespace Chameleon.lib.Common.Models;

public class UserProfile {
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

	private ProxySettings _proxy = new();
	public ProxySettings Proxy {
		get => _proxy;
		set => _proxy = value ?? new ProxySettings();
	}
}