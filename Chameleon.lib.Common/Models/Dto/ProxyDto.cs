namespace Chameleon.lib.Common.Models.Dto;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class ProxDto : Interfaces.Dto {
	public string? host { get; set; }
	public int port { get; set; }
	public string? userName { get; set; }
	public string? password { get; set; }
}

public class ProxCreditDto : Interfaces.Dto {
	public decimal Amount { get; set; }
}

public class ProxCountryDto : Interfaces.Dto { 
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