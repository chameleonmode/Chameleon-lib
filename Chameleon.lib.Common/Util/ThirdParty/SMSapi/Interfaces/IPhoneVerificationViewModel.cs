using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Common.Util.ThirdParty.SMSapi.Interfaces;
public interface IPhoneVerificationViewModel
		: IAmaViewModel,
		ISingletonDependency {
	IPVApiModel CodesVerify { get; }
	IPVApiModel SMSPVA { get; }
	IPVApiModel SMSPool { get; }
}
