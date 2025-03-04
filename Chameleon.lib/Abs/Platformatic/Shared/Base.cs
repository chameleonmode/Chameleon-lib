using static Chameleon.lib.Abs.Platformatic.Shared.Client;

namespace Chameleon.lib.Abs.Platformatic.Shared;
public abstract class Base {
  public static Client Client { get; } = Instance;
	public static Task<T?> Get<T>(string path, Request? @params = null) =>
		Instance.Get<T>(path, @params);
	public static Task<T?> Post<T>(string path, Request @params) =>
		Instance.Post<T>(path, @params);
	public static Task<T?> Put<T>(string path, Request @params) =>
		Instance.Put<T>(path, @params);
	public static Task<T?> Delete<T>(string path, Request? @params = null) =>
		Instance.Delete<T>(path, @params);
}
