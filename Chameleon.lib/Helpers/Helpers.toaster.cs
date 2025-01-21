using Chameleon.lib.Interfaces.Services;

namespace Chameleon.lib.Helpers;
public class Toaster {
	//
	public IToasterService? ToastNotificationService { get; } = IoC.GetService<IToasterService>();

	//
	public static void Error(params string[] err) =>
		Instance.ToastNotificationService?.ShowError(string.Join(": ", err));
	public static void Success(string err) =>
		Instance.ToastNotificationService?.ShowSuccess(string.Join(": ", err));
	public static void Info(string msg) =>
		Instance.ToastNotificationService?.ShowInformation(string.Join(": ", msg));

	// Singleton
	private Toaster() { }
	public static Toaster Instance { get; } = new Toaster();
}
