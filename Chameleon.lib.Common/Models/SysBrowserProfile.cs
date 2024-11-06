namespace Chameleon.lib.Common.Models;

public class SysBrowserProfile {
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

	private SysBrowserProxy _proxy = new();
	public SysBrowserProxy Proxy {
		get => _proxy;
		set => _proxy = value ?? new SysBrowserProxy();
	}
}