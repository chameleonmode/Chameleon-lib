using System.Net.Http.Headers;
using Chameleon.lib.Util;

namespace Chameleon.lib.ThirdParty.PVA;

public class OrderBase {
	public int success { get; set; }
	public string? message { get; set; }
}

public class SMSOrder {
	public object? status { get; set; }
	public object? message { get; set; }
	public object? sms { get; set; }
	public object? full_sms { get; set; }
	public object? resend { get; set; }
	public object? expiration { get; set; }
	public object? time_left { get; set; }
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

public class SMSPoolAPI : PVAInstance {
	public record Country(int ID, string Name, string Short_name, string Region) : RCountry(Name);
	public record Service(int ID, string Name, int Favourite) : RService(Name);
	private AuthenticationHeaderValue Authorization => new("Token", ApiKey);

	public override async Task Init() {
		ApiKey = IoC.GetValue(string.Join('_', nameof(SMSPoolAPI), nameof(ApiKey)));

		var getCountriesUrl = $"https://api.smspool.net/country/retrieve_all";
		var countriesResponse = await GetAsync(getCountriesUrl);
		if (countriesResponse != null && JSON.Deserialize<Country[]>(countriesResponse) is Country[] countries)
			Countries = countries;

		var getServicesUrl = $"https://api.smspool.net/service/retrieve_all";
		var servicesResponse = await GetAsync(getServicesUrl);
		if (servicesResponse != null && JSON.Deserialize<Service[]>(servicesResponse) is Service[] services)
			Services = services;
	}

	public override async Task Save() {
		await IoC.SetValue<string>(ApiKey ?? "", string.Join('_', nameof(SMSPoolAPI), nameof(ApiKey)));
	}

	public override Task<Tuple<string, string>> GetNumberAsync(RCountry country, RService app)
			=> OrderSMSAsync((Country)country, (Service)app);

	private async Task<Tuple<string, string>> OrderSMSAsync<T1, T2>(T1 country, T2 service)
	where T1 : Country
	where T2 : Service {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		var apiUrl = $"https://api.smspool.net/purchase/sms";
		using var response = await PostAsync(apiUrl, Authorization, null, new MultipartFormDataContent
		{
			{ new StringContent(country.ID.ToString()), "country" },
			{ new StringContent(service.ID.ToString()), "service" },
			{ new StringContent(ApiKey), "key" }
		});
		var responseContent = await response.Content.ReadAsStringAsync();
		return response.IsSuccessStatusCode &&
				JSON.Deserialize<SuccessfullOrder>(responseContent) is SuccessfullOrder successfullOrder
			? new Tuple<string, string>(
					JSON.Serialize(successfullOrder),
					successfullOrder.number.ToString()
				)
			: JSON.Deserialize<UnSuccessfullOrder>(responseContent) is UnSuccessfullOrder unsuccessfullOrder
				? new Tuple<string, string>(
								JSON.Serialize(unsuccessfullOrder),
								unsuccessfullOrder.message ?? "Failed to read reason for unsuccessfull order"
							)
				: new Tuple<string, string>(responseContent, response.StatusCode.ToString());
	}

	public override async Task<Tuple<string, string>> CancelOrderAsync(string orderid) {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		if (JSON.Deserialize<SuccessfullOrder>(orderid) is SuccessfullOrder phoneNumberData && phoneNumberData.order_id != null) {
			var apiUrl = "https://api.smspool.net/sms/cancel";
			using var response = await PostAsync(apiUrl, Authorization, null, new MultipartFormDataContent
			{
				{ new StringContent(phoneNumberData.order_id), "orderid" },
				{ new StringContent(ApiKey), "key" }
			});
			var responseContent = await response.Content.ReadAsStringAsync();
			var jsonResponse = JSON.Deserialize<OrderBase>(responseContent);
			var formattedJson = JSON.Stringify(jsonResponse);
			return new Tuple<string, string>(formattedJson ?? string.Empty, (jsonResponse?.success > 0).ToString());
		} else {
			return new Tuple<string, string>("", "Failed to deserialize orderid");
		}
	}

	public override async Task<Tuple<string, string>> GetCodeAsync(RCountry country, RService app, string numberData) {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		if (JSON.Deserialize<SuccessfullOrder>(numberData) is SuccessfullOrder phoneNumberData && phoneNumberData.order_id != null) {
			var apiUrl = "https://api.smspool.net/sms/check";
			using var response = await PostAsync(apiUrl, Authorization, null, new MultipartFormDataContent
			{
				{ new StringContent(phoneNumberData.order_id), "orderid" },
				{ new StringContent(ApiKey), "key" }
			});
			var content = await response.Content.ReadAsStringAsync();

			return
				response.IsSuccessStatusCode &&
				JSON.Deserialize<SMSOrder>(content) is { } order &&
				order.sms?.ToString() is { } sms
					? new Tuple<string, string>(JSON.Serialize(order), sms)
					: JSON.Deserialize<UnSuccessfullOrder>(content) is { } fail
						? new Tuple<string, string>(JSON.Serialize(fail), fail.message ?? "Failed to read reason for unsuccessfull order")
						: new Tuple<string, string>(content, response.StatusCode.ToString());
		}
		return new Tuple<string, string>("", "Failed to deserialize orderid");
	}

	private SMSPoolAPI()
			: base("SMS Pool", [], []) {
	}
	public static SMSPoolAPI Instance { get; } = new SMSPoolAPI();
}
