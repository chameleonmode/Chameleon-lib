using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chameleon.lib.Common.Interfaces.Services;

namespace Chameleon.lib.Common.ServiceManagers;
public class CopyPasta {

	private readonly ICopyPastaService? _copyPastaService;
	private CopyPasta()
	{
		 _copyPastaService = IoC.GetService<ICopyPastaService>();
	}
	public static Task Copy(string text)
	{
		return Instance._copyPastaService!.SetTextAsync(text);
	}
	public static CopyPasta Instance { get; } = new CopyPasta();
}
