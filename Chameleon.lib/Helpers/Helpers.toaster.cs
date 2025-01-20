using Chameleon.lib.Interfaces.Services;

namespace Chameleon.lib.Helpers;
public class Toaster {
	//
	public IToasterService? ToastNotificationService { get; } = IoC.GetService<IToasterService>();

	//
	public static void Error(params string[] err) =>
		Current.ToastNotificationService?.ShowError(string.Join(": ", err));
	public static void Success(string err) =>
		Current.ToastNotificationService?.ShowSuccess(string.Join(": ", err));
	public static void Info(string msg) =>
		Current.ToastNotificationService?.ShowInformation(string.Join(": ", msg));

	// Singleton
	private Toaster() { }
	public static Toaster Current { get; } = new Toaster();
}
