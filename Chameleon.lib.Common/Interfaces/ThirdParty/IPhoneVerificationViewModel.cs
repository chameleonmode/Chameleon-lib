using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Common.Interfaces.ThirdParty;
public interface IPhoneVerificationViewModel
		: IAmaViewModel,
		ISingletonDependency {
	IPVApiModel CodesVerify { get; }
	IPVApiModel SMSPVA { get; }
}
