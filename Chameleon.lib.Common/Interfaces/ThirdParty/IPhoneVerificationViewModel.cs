using Chameleon.Interfaces;
using Chameleon.Interfaces.Ioc;

namespace Chameleon.lib.Common.Interfaces.ThirdParty;
public interface IPhoneVerificationViewModel
		: IPageViewModel,
		ISingletonDependency {
	IPVApiModel CodesVerify { get; }
	IPVApiModel SMSPVA { get; }
}
