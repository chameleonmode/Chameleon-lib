using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface ICopyPastaService {
	Task SetTextAsync(string text);
}
