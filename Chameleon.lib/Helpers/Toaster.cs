using System.Diagnostics;
using Chameleon.lib.Services;

namespace Chameleon.lib.Helpers;
public class Toaster {
	public IToasterService? ToastNotificationService { get; } = IoC.GetService<IToasterService>();

	// Format the message
	static string Format(params string[] msg) {
		var message = string.Join('\n', msg)
		.Replace("\nObject reference not set to an instance of an object.", "");
		Debug.WriteLine(message);
		return message;
	}
	
	// Show the message
	public static void Error(params string[] err) {
		Instance.ToastNotificationService?.ShowError(Format(err));
	}
	public static void Success(params string[] msg) {
		Instance.ToastNotificationService?.ShowSuccess(Format(msg));
	}
	public static void Info(params string[] msg) {
		Instance.ToastNotificationService?.ShowInformation(Format(msg));
	}

	// Singleton
	private Toaster() { }
	public static Toaster Instance { get; } = new Toaster();
}