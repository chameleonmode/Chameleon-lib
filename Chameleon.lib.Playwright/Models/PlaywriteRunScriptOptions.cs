using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Playwright.Models;
public class PlaywriteRunScriptOptions : IPlaywriteRunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Chromium;
	public IAutomationScriptDescription? Script { get; set; }
	public IBundledScript? BundledScript { get; set; }
}
