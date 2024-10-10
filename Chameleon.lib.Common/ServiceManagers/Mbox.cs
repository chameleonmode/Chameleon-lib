using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class Mbox {
	private readonly IMboxService? MboxService;

	public static async Task<bool> Show(string title, string content, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo, string? fontIconInfo = null, Enums.MboxResult retVal = Enums.MboxResult.Primary)
	{
		return await Instance.MboxService!.Show(title, content, btns, fontIconInfo ?? "Info") == retVal;
	}
	public static Task<bool> ShowErrorAsync(string title, string content)
	{
		return Show(title, content, Enums.MBoxButtons.Ok, "Error");
	}

	public static Task<Enums.TaskDialogResult> ShowTaskDialog<TViewModel>(Func<TViewModel> initialize, object content, string header,
		string? subHeader = null, string title = Consts.AppName, object? footer = null, 
		Enums.Symbas symbas = Enums.Symbas.Alert, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo)
	{
		return Instance.MboxService!.ShowTaskDialog(initialize, content, header, subHeader, title, footer, symbas, btns);
	}
	public static Mbox Instance { get; } = new Mbox();
	private Mbox()
	{
		MboxService = IoC.GetService<IMboxService>();
	}
}
