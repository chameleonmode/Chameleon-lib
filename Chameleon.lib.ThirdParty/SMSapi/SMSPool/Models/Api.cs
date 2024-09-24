namespace Chameleon.lib.ThirdParty.SMSapi.SMSPool.Models;
public class OrderBase {
	public int success { get; set; }
	public string? message { get; set; }
}

public class SMSOrder {
	public int status { get; set; }
	public string? message { get; set; }
	public string? sms { get; set; }
	public string? full_sms { get; set; }
	public int resend { get; set; }
	public long expiration { get; set; }
	public long time_left { get; set; }
}

public class SuccessfullOrder : OrderBase {
	public long number { get; set; }
	public string? cc { get; set; }
	public string? phonenumber { get; set; }
	public string? order_id { get; set; }
	public string? country { get; set; }
	public string? service { get; set; }
	public long pool { get; set; }
	public object? expires_in { get; set; }
	public long expiration { get; set; }
	public string? cost { get; set; }
	public int cost_in_cents { get; set; }
}

public class UnSuccessfullOrder : OrderBase {
	public Pools? pools { get; set; }
	public Error1[]? errors { get; set; }
	public string? type { get; set; }
}

public class Pools {
	public Foxtrot? Foxtrot { get; set; }
}

public class Foxtrot {
	public int success { get; set; }
	public string? message { get; set; }
	public Error[]? errors { get; set; }
	public string? type { get; set; }
}

public class Error {
	public string? message { get; set; }
}

public class Error1 {
	public string? message { get; set; }
}

