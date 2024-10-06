using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class Mbox {
	private readonly IMboxService? MboxService;

	public static async Task<bool> ShowAsync(string title, string content, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo, string? fontIconInfo = null, Enums.MboxResult retVal = Enums.MboxResult.Primary)
	{
		return await Instance.MboxService!.ShowAsync(title, content, btns, fontIconInfo ?? "Info") == retVal;
	}
	public static Task<bool> ShowErrorAsync(string title, string content)
	{
		return ShowAsync(title, content, Enums.MBoxButtons.Ok, "Error");
	}

	public static Mbox Instance { get; } = new Mbox();
	private Mbox()
	{
		MboxService = IoC.GetService<IMboxService>();
	}
}
