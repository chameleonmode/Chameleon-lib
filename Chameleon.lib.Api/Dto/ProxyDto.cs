namespace Chameleon.lib.Api.Dto;

public class ProxCreditDto : Dto {
	public decimal Amount { get; set; }
}

public class ProxCountryDto : Dto { 
	public string? Name { get; set; }
}

public class ProxAccessDto : ProxDto {
	public string? Url { get; set; }
}
public class ProxyCreditOrderDto {
	public Guid Id { get; set; }
	public decimal Amount { get; set; }
	public string? Url { get; set; }
}