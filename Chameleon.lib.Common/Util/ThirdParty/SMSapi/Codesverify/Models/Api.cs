namespace Chameleon.lib.Common.Util.ThirdParty.SMSapi.Codesverify.Models;
public class ApiGetNumberResponse {
	public string? Number { get; set; }

	public List<string> Errors { get; } =
	[
			"Customer Not Found.",
				"App Not Found.",
				"Country Not Found.",
				"New Numbers registration in progress, please wait or check back later.",
				"Error 102, check back later."
	];
}

public class ApiGetCodeResponse {
	public string? Code { get; set; }

	public List<string> Errors { get; } =
	[
			"Customer Not Found.",
				"Number Not Found.",
				"You have not received any code yet.",
				"Your balance is expired.",
				"Error 102, check back later."
	];
}
