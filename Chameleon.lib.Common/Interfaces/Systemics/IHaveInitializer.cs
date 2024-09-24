using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Interfaces.Systemics;
public interface IHaveInitializer {
	TaskCompletionSource LoadedTCS { get; }
	Task InvokeInitializeAsyncCommand(object? param);
}
