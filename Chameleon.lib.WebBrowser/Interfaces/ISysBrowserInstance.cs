using System.Diagnostics;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.WebBrowser.Models;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface ISysBrowserInstance : IAmInitializer, IDisposable {
	public event EventHandler<SysBrowserLaunchOptions>? OnProcessClosed;
	public event EventHandler<SysBrowserLaunchOptions>? OnProcessOpenError;
	public event EventHandler<SysBrowserLaunchOptions>? OnBecameForeground;

	abstract SystemBrowserType BrowserType { get; set; }
	Process? Brocess { get; set; }
	void MakeForeground();
}
