using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Interfaces.Systemics;
public interface IAmaViewModel : IHaveInitializer {
	string Title { get; set; }
	Task OnNavigatedToAsync(object? param);
}