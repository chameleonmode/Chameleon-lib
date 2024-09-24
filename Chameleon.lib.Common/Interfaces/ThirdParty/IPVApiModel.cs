using Chameleon.lib.Common.Interfaces.Systemics;
using Chameleon.lib.Common.Records;

namespace Chameleon.lib.Common.Interfaces.ThirdParty;
public interface IPVApiModel : IAmaViewModel { 
	string? ApiKey { get; set; }

	string? GetNumberData { get; set; }
	string? ReceiveSMSData { get; set; }
	string? LastFormatedResponse { get; set; }

	bool IsVisible { get; set; }
	bool IsVisibleSave { get; set; }
	bool IsAwaiting { get; set; }
	bool CanCancel { get; set; }

	IList<RCountry>? Countries { get; set; }
	RCountry? SelectedCountry { get; set; }
	IList<RService>? Apps { get; set; }
	RService? SelectedApp { get; set; }
}
