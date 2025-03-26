using System.Diagnostics;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.WebBrowser.Models;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface IBrowserInstance : IAmInitializer {
	event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	Process? Brocess { get; set; }
	SysBrowserSettings Settings { get; init; }
	string SessionId { get; }
	void InvokeEvent(Enums.SysBrowserEventType eventType);
	void Close();
	Task Start();
}
