using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.WebBrowser.Models;

public record SysBrowserEvent(SysBrowserOpenOptions OpenOptions, SysBrowserEventType EventType);

