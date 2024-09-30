using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class Toaster {
	private readonly IToasterService? ToastNotificationService;

	private Toaster()
	{
		ToastNotificationService = IoC.GetService<IToasterService>();
	}
	public static Toaster Current { get; } = new Toaster();

	public static void ShowErr(params string[] err)
	{
		//Chameleon.Common.Helpers.ToasterHelper.ShowErr(string.Join(": ", err));
		Current.ToastNotificationService?.ShowError(string.Join(": ", err));
	}

	public static void ShowSuccess(string err) =>
		Current.ToastNotificationService?.ShowSuccess(string.Join(": ", err));
}
