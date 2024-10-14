using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Models.Interfaces;
public interface IHasid {
#pragma warning disable IDE1006 // Naming Styles
	int id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}
