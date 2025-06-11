using Chameleon.lib.Api.Dto;

namespace Chameleon.lib.Api.Repos;
public class ProxyAccessRepo : ApiBase<ProxAccessDto> {
	private ProxyAccessRepo() : base(Consts.Api.Endpoints.Proxy) { }

	public static Task<ProxAccessDto[]> GetAccess(ProxyAccessRequestDto input)
	{
		return Instance.Get<ProxAccessDto[]>($"GetAccess?IpType={input.IpType}&ProtocolType={input.ProtocolType}&HostType={input.HostType}&CountryId={input.CountryId}&Count={input.Count}");
	}

	public static Task<ProxCountryDto[]> GetCountries()
	{
		return Instance.Get<ProxCountryDto[]>("GetCountries");
	}

	public static ProxyAccessRepo Instance { get; } = new ProxyAccessRepo();
}

public class ProxyCreditRepo : ApiBase<ProxCreditDto> {
	private ProxyCreditRepo() : base(Consts.Api.Endpoints.ProxyCredit) { }

	//public ProxyCreditDto BuyCredits(BuyCreditsDto input)
	//{
	//	var dto = _apiClient.Post<ProxyCreditDto>(GetEndpointUrl("BuyCredits"), input);
	//	ThrowIfInvalidId(dto);
	//	return dto;
	//}

	public static Task<ProxyCreditOrderDto> CreateOrder(decimal amount) => Instance.Post<ProxyCreditOrderDto>("CreateOrder", new { Amount = amount });
	public static Task<ProxCreditDto> GetCredits() => Instance.Get<ProxCreditDto>("GetCredits");

	public static ProxyCreditRepo Instance { get; } = new ProxyCreditRepo();
}
