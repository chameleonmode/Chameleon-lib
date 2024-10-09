using System.Diagnostics;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Sys;
using Chameleon.lib.WebBrowser.Models;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface ISysBrowserInstance : IAmInitializer
{
  public event Delegatorz.Event<SysBrowserEvent>? OnEvent;

  SysBrowserSettings Settings { get; init; }
	void InvokeEvent(Enums.SysBrowserEventType eventType);
	void Close();
}
