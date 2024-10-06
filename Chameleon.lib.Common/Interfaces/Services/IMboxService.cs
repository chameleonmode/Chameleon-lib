using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface IMboxService {
	Task<MboxResult> ShowAsync(string title,
		string content,
		MBoxButtons btns = MBoxButtons.YesNo,
		string icon = "Info");
}
