using System.Text.RegularExpressions;

namespace Chameleon.lib.Common.Constants;
public static partial class Regexers {
	[GeneratedRegex(@"user_pref\(""(.*?)"", (\""(.*?)\""|.*?)\);")]
	public static partial Regex UserPrefRegex();
}
