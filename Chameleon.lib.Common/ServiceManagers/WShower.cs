using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class DialogBox {
	private readonly IShowWindowService? WShowerService;
	public static void ShowTopmost<TView, TViewModel>(Action<TViewModel> initialize, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256) where TView : new() where TViewModel : new()
	{
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(initialize, onClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, Action<TViewModel>? initialize = default, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256) where TView : new()
	{
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(vm, initialize ??= _=>{ }, onClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, TView v, Action<TViewModel> initialize, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256)
	{
		Instance.WShowerService?.ShowTopmost(vm, v, initialize, onClosed, title, width);
	}

	public static DialogBox Instance { get; } = new DialogBox();
	private DialogBox()
	{
		WShowerService = IoC.GetService<IShowWindowService>();
	}
}
