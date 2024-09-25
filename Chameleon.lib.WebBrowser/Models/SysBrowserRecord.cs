using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.WebBrowser.Models;

public record class SysBrowserRecord(string Name, string Path) {
	public override string ToString()
	{
		return Name ?? Path;
	}
}
