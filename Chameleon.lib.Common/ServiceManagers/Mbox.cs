using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class Mbox {
	private readonly IMboxService? MboxService;

	public static Task<Enums.MboxResult> ShowErrorAsync(string title, string content)
	{
		return Instance.MboxService!.ShowAsync(title, content, Enums.MBoxButtons.Ok, "Error");
	}

	public static Mbox Instance { get; } = new Mbox();
	private Mbox()
	{
		MboxService = IoC.GetService<IMboxService>();
	}
}
