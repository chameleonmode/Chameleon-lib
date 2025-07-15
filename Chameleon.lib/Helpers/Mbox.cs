using Chameleon.lib.Services;

namespace Chameleon.lib.Helpers;

public class MessageBox {
	private readonly IMboxService MboxService;
	private readonly IDispatchService dispatcher;
	MessageBox() {
		MboxService = IoC.GetService<IMboxService>()!;
		dispatcher = IoC.GetService<IDispatchService>()
			?? throw new InvalidOperationException("IDispatchService is not registered in the IoC container.");
	}
	public static MessageBox Instance { get; } = new MessageBox();

	public static async Task<bool> Show(string title, string content,
		MBoxButtons btns = MBoxButtons.YesNo, string? icon = null, MboxResult retVal = MboxResult.Primary)
	=> await Instance.MboxService.Show(title, content, btns, icon ?? "Info") == retVal;

	public static async Task<bool> Error(string title, string content, Exception? ex = null) =>
		await Show(title, content + (ex != null ? $"\n{(ex.Message.Contains('\n') ? ex.Message[ex.Message.LastIndexOf('\n')..] : ex.Message)}" : ""), MBoxButtons.OkCancel, "Error");

	public record Options<TViewModel>(Func<TViewModel> Initialize, string Header,
		 string? SubHeader = null, string Title = IoC.AppName, object? Footer = null, Symbas Symbas = Symbas.Alert, MBoxButtons Btns = MBoxButtons.YesNo
	);
	public record Options(string Header, string? SubHeader = null, string Title = IoC.AppName,
		 object? Footer = null, Symbas Symbas = Symbas.Alert, MBoxButtons Btns = MBoxButtons.YesNo
	);
	public static async Task<TViewModel?> Show<TView, TViewModel>(TViewModel vm, Options parameters) where TView : new() {
		var result = await Instance.dispatcher.InvokeOnUiThread(async () => await Instance.MboxService.ShowTaskDialog(
			 () => vm,
			 new TView(),
			 parameters.Header,
			 parameters.SubHeader,
			 parameters.Title,
			 parameters.Footer,
			 parameters.Symbas,
			 MBoxButtons.OkCancel)
		);
		return result is TaskDialogResult.OK or TaskDialogResult.Yes ? vm : default;
	}

	public static Task<TaskDialogResult> ShowTaskDialog<TView, TViewModel>(Options<TViewModel> parameters) where TView : new() {
		return Instance.MboxService.ShowTaskDialog(parameters.Initialize, new TView(), parameters.Header, parameters.SubHeader, parameters.Title, parameters.Footer, parameters.Symbas, parameters.Btns);
	}
}
public class DialogBox {
	private readonly IShowWindowService WShowerService;
	public static void ShowTopmost<TView, TViewModel>(Action<TViewModel> initialize, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256) where TView : new() where TViewModel : new() {
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(initialize, onClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, Action<TViewModel>? initialize = default, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256) where TView : new() {
		Instance.WShowerService?.ShowTopmost<TView, TViewModel>(vm, initialize ??= _ => { }, onClosed, title, width);
	}
	public static void ShowTopmost<TView, TViewModel>(TViewModel vm, TView v, Action<TViewModel> initialize, Action<TViewModel>? onClosed = null, string title = "TP", int width = 256) {
		Instance.WShowerService?.ShowTopmost(vm, v, initialize, onClosed, title, width);
	}

	public static DialogBox Instance { get; } = new DialogBox();
	DialogBox() {
		WShowerService = IoC.GetService<IShowWindowService>()
			?? throw new InvalidOperationException("IShowWindowService is not registered in the IoC container.");
	}
}