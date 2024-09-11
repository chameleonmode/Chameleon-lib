using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Interfaces;
public interface IToastNotificationService
				: ISingletonDependency {
		void SetHostWindow(object? hostWindow);
		void ShowInformation(string message);
		void ShowError(string message);
		void ShowSuccess(string message);
		void ShowWarning(string message);
		void ClearAllMessages();
}
