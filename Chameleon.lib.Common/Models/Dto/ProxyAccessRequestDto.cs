namespace Chameleon.lib.Common.Models.Dto;

public enum ProxyHostType { IpAddress, Hostname }
public enum ProxyIpType { Random, Sticky }
public enum ProxyProtocolType { Http, Ssl }
public class ProxyAccessRequestDto {
	public ProxyIpType IpType { get; set; }
	public ProxyProtocolType ProtocolType { get; set; }
	public ProxyHostType HostType { get; set; }
	public int? CountryId { get; set; }
	public int Count { get; set; }
}
