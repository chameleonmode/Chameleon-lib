using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Extensions;
public static class BoolExts {
	public static string Tlwr(this bool value)
	{
		return value.ToString().ToLower();
	}
}
