using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class Navigator {
	public readonly INavigationService? NavigationService;
	public static Navigator Instance { get; } = new Navigator();
	private Navigator()
	{
		NavigationService = IoC.GetService<INavigationService>();
	}

	public static void SetFrame(object f)
	{
		Instance.NavigationService?.SetFrame(f);
	}

	public static void NavigateToType(Type t, object? parameter = null)
	{
		Instance.NavigationService?.NavigateToType(t, parameter);
	}
}
