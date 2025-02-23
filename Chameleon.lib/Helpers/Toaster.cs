using System.Diagnostics;
using Chameleon.lib.Interfaces.Services;

namespace Chameleon.lib.Helpers;
public class Toaster {
	public IToasterService? ToastNotificationService { get; } = IoC.GetService<IToasterService>();
	// Singleton
	private Toaster() { }
	public static Toaster Instance { get; } = new Toaster();
	//
	static string Format(params string[] msg) => string.Join(": ", msg);
	//
	public static void Error(params string[] err) {
		var txt = Format(err);
		if(Instance.ToastNotificationService == null){
			Debug.WriteLine(txt);
		}else{
			Instance.ToastNotificationService?.ShowError(txt);
		}
	}
	public static void Success(params string[] msg) {
		var txt = Format(msg);
		if(Instance.ToastNotificationService == null){
			Debug.WriteLine(txt);
		}else{
			Instance.ToastNotificationService?.ShowSuccess(txt);
		}
	}
	public static void Info(params string[] msg) {
		var txt = Format(msg);
		if(Instance.ToastNotificationService == null){
			Debug.WriteLine(txt);
		}else{
			Instance.ToastNotificationService?.ShowInformation(txt);
		}
	}
}