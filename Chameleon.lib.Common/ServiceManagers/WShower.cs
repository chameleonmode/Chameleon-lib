using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class WShower {
	private readonly IShowWindowService? WShowerService;
	public static void ShowTopmost<TView, TViewModel>(Action<TViewModel> initialize, Action<TViewModel>? OnClosed = null, string title = "TP", int width = 256) where TView : new() where TViewModel : new()
	{
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(initialize, OnClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, Action<TViewModel> initialize, Action<TViewModel>? OnClosed = null, string title = "TP", int width = 256) where TView : new()
	{
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(vm, initialize, OnClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, TView v, Action<TViewModel> initialize, Action<TViewModel>? OnClosed = null, string title = "TP", int width = 256)
	{
		Instance.WShowerService?.ShowTopmost(vm, v, initialize, OnClosed, title, width);
	}

	public static WShower Instance { get; } = new WShower();
	private WShower()
	{
		WShowerService = IoC.GetService<IShowWindowService>();
	}
}
