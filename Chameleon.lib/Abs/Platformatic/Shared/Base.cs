using static Chameleon.lib.Abs.Platformatic.Shared.Client;

namespace Chameleon.lib.Abs.Platformatic.Shared;
public abstract class Base {
	public static Client Client => Instance;
	public static Task<T?> Get<T>(string path, Request? request = null)
		=> Client.SendRequestAsync<T>(HttpMethod.Get, path, request ?? new());
	public static Task<T?> Post<T>(string path, Request request)
		=> Client.SendRequestAsync<T>(HttpMethod.Post, path, request);
	public static Task<T?> Put<T>(string path, Request request)
		=> Client.SendRequestAsync<T>(HttpMethod.Put, path, request);
	public static Task<T?> Delete<T>(string path, Request? request = null)
		=> Client.SendRequestAsync<T>(HttpMethod.Delete, path, request ?? new());
}
