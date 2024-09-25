using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.Managers;
public class Toaster {
	private readonly IToasterService? ToastNotificationService;
	private readonly IDispatcherService? DispatcherService;

	private Toaster()
	{
		ToastNotificationService = IoC.GetService<IToasterService>();
		DispatcherService = IoC.GetService<IDispatcherService>();
	}
	public static Toaster Current { get; } = new Toaster();

	public static void ShowErr(params string[] err)
	{
		InvokeOnUi(()=>Current.ToastNotificationService?.ShowError(string.Join(": ", err)));
	}

	public static void ShowSuccess(string err) =>
		InvokeOnUi(()=>Current.ToastNotificationService?.ShowSuccess(err));

	private static void InvokeOnUi(Action action) => 
		Current.DispatcherService?.InvokeOnUiThread(action);
}
