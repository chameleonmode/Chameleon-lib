using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Playwright.Scripts;
internal static class ScriptsUtil {
	internal static IDictionary<string, string> ParseArguments(this IList<IAutomationParameterValue>? pargs) {
		ArgumentNullException.ThrowIfNull(pargs);
		return pargs
			.Where(x => x.Name != null && x.Value != null)
			.ToDictionary(x => x.Name!, x => x.Value!);
	}
}
