using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models;

namespace Chameleon.lib.Common.Interfaces.Sys;
public interface ISysBrowserInstance : IAmInitializer {
	public event Delegatorz.Event<SysBrowserEvent>? OnEvent;
	SysBrowserSettings Settings { get; init; }
	void InvokeEvent(Enums.SysBrowserEventType eventType);
	void Close();
}
