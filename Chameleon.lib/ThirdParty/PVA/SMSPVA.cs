using Chameleon.lib.Util;

using System.Text.Json.Serialization;

namespace Chameleon.lib.ThirdParty.PVA;

public class ApiResponse<T> {
	[JsonPropertyName("statusCode")]
	public int StatusCode { get; set; }

	[JsonPropertyName("data")]
	public T? Data { get; set; }

	[JsonPropertyName("error")]
	public ErrorData? Error { get; set; }
}
public class DataBase {
	[JsonPropertyName("orderId")]
	public int OrderId { get; set; }
}

public class GetNumberData : DataBase {

	[JsonPropertyName("phoneNumber")]
	public string? PhoneNumber { get; set; }

	[JsonPropertyName("countryCode")]
	public string? CountryCode { get; set; }

	[JsonPropertyName("orderExpireIn")]
	public int OrderExpireIn { get; set; }
}

public class ReceiveSMSData : DataBase {
	[JsonPropertyName("sms")]
	public Sms? Sms { get; set; }

	[JsonPropertyName("orderExpireIn")]
	public int OrderExpireIn { get; set; }
}

public class Sms {
	[JsonPropertyName("code")]
	public string? Code { get; set; }
	[JsonPropertyName("fullText")]
	public string? FullText { get; set; }
}

public class ErrorData {
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }
}

public class SMSPVAPI : PVAInstance {
	public record class Service(int ID, string Logo, string Name, string Code) : RService(Name);
	public record class Country(int ID, string Name, string Code) : RCountry(Name);
	private List<KeyValuePair<string, string>> ApiKeyHeaders => [new("apikey", ApiKey!)];

	public override Task Init() {
		ApiKey = IoC.GetValue(string.Join('_', nameof(SMSPVAPI), nameof(ApiKey)));
		return Task.CompletedTask;
	}

	public override async Task Save() {
		await IoC.SetValue<string>(ApiKey ?? "", string.Join('_', nameof(SMSPVAPI), nameof(ApiKey)));
	}

	public override Task<Tuple<string, string>> GetNumberAsync(RCountry country, RService app)
			=> GetActivationNumberAsync((Country)country, (Service)app);

	private async Task<Tuple<string, string>> GetActivationNumberAsync<T1, T2>(T1 country, T2 service)
			where T1 : Country
			where T2 : Service {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		var url = $"https://api.smspva.com/activation/number/{country.Code}/{service.Code}";
		var responseBody = await GetAsync(url, ApiKeyHeaders);
		var jsonResponse = JSON.Deserialize<ApiResponse<GetNumberData>>(responseBody);

		return new Tuple<string, string>(responseBody, jsonResponse?.Data?.PhoneNumber ?? "(+x)-xxx-xxx-xxxx");
	}

	public override async Task<Tuple<string, string>> GetCodeAsync(RCountry country, RService app, string numberData) {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		if (JSON.Deserialize<ApiResponse<GetNumberData>>(numberData)?.Data?.OrderId is int oId) {
			var url = $"https://api.smspva.com/activation/sms/{oId}";

			var responseBody = await GetAsync(url, ApiKeyHeaders);
			var responseData = JSON.Deserialize<ApiResponse<ReceiveSMSData>>(responseBody);
			return new Tuple<string, string>(responseBody, responseData?.Data?.Sms?.Code ?? "xxx-xxx");
		} else {
			return new Tuple<string, string>("", "Failed to find orderid");
		}
	}

	public override async Task<Tuple<string, string>> CancelOrderAsync(string orderId) {
		ArgumentNullException.ThrowIfNull(ApiKey, nameof(ApiKey));

		if (JSON.Deserialize<ApiResponse<GetNumberData>>(orderId)?.Data?.OrderId is int oId) {
			var url = $"https://api.smspva.com/activation/cancelorder/{oId}";

			var responseContent = await PutAsync(url, ApiKeyHeaders);
			var jsonResponse = JSON.Deserialize<ApiResponse<DataBase>>(responseContent);
			return new Tuple<string, string>(responseContent, (jsonResponse?.Error?.Type == null).ToString());
		} else {
			return new Tuple<string, string>("", "Failed to find orderid");
		}
	}

	private SMSPVAPI()
			: base(
				"SMS PVA",
				[
					new Country(1, "United States", "US"),
						new Country(2, "Canada", "CA"),
						new Country(3, "Unt. Kingdom", "UK"),
						new Country(4, "France", "FR"),
						new Country(5, "Germany", "DE"),
						new Country(6, "Italy", "IT"),
						new Country(7, "Spain", "ES"),
						new Country(8, "Albania", "AL"),
						new Country(9, "Argentina", "AR"),
						new Country(10, "Australia", "AU"),
						new Country(11, "Austria", "AT"),
						new Country(12, "Bangladesh", "BD"),
						new Country(13, "Bos. and Herz.", "BA"),
						new Country(14, "Brazil", "BR"),
						new Country(15, "Bulgaria", "BG"),
						new Country(16, "Cambodia", "KH"),
						new Country(17, "Chile", "CL"),
						new Country(18, "Colombia", "CO"),
						new Country(19, "Croatia", "HR"),
						new Country(20, "Cyprus", "CY"),
						new Country(21, "Czech Republic", "CZ"),
						new Country(22, "Denmark", "DK"),
						new Country(23, "Dominicana", "DO"),
						new Country(24, "Egypt", "EG"),
						new Country(25, "Estonia", "EE"),
						new Country(26, "Finland", "FI"),
						new Country(27, "Georgia", "GE"),
						new Country(28, "Ghana (Virtual)", "GH"),
						new Country(29, "Gibraltar", "GI"),
						new Country(30, "Greece", "GR"),
						new Country(31, "Hong Kong", "HK"),
						new Country(32, "Hungary", "HU"),
						new Country(33, "India", "IN"),
						new Country(34, "Japan", "JP"),
						new Country(35, "Kyrgyzstan (Virtual)", "KG"),
						new Country(36, "Malta", "MT"),
						new Country(37, "Norway", "NO"),
						new Country(38, "Pakistan (Virtual)", "PK"),
						new Country(39, "Singapore", "SG"),
						new Country(40, "Tanzania", "TZ"),
						new Country(41, "Uzbekistan (Virtual)", "UZ"),
						new Country(42, "Indonesia", "ID"),
						new Country(43, "Ireland", "IE"),
						new Country(44, "Israel", "IL"),
						new Country(45, "Kazakhstan", "KZ"),
						new Country(46, "Kenya", "KE"),
						new Country(47, "Laos", "LA"),
						new Country(48, "Latvia", "LV"),
						new Country(49, "Lithuania", "LT"),
						new Country(50, "Macedonia", "MK"),
						new Country(51, "Malaysia", "MY"),
						new Country(52, "Mexico", "MX"),
						new Country(53, "Morocco", "MA"),
						new Country(54, "Netherlands", "NL"),
						new Country(55, "New Zealand", "NZ"),
						new Country(56, "Nigeria", "NG"),
						new Country(57, "Paraguay", "PY"),
						new Country(58, "Philippines", "PH"),
						new Country(59, "Poland", "PL"),
						new Country(60, "Portugal", "PT"),
						new Country(61, "Romania", "RO"),
						new Country(62, "Russian Federation", "RU"),
						new Country(63, "Serbia", "RS"),
						new Country(64, "Slovakia", "SK"),
						new Country(65, "Slovenia", "SI"),
						new Country(66, "South Africa", "ZA"),
						new Country(67, "Sweden", "SE"),
						new Country(68, "Thailand", "TH"),
						new Country(69, "Turkey", "TR"),
						new Country(70, "Ukraine", "UA"),
						new Country(71, "Vietnam", "VN")
		],
				[
				new Service(1, "", "OpenAI API (chatGPT, DALL-e 2)", "opt132"),
						new Service(2, "", "22bet", "opt224"),
						new Service(3, "", "888casino", "opt22"),
						new Service(4, "", "Abbott", "opt242"),
						new Service(5, "", "Adidas & Nike", "opt86"),
						new Service(6, "", "Airbnb", "opt46"),
						new Service(7, "", "Alibaba (Taobao, 1688.com)", "opt61"),
						new Service(8, "", "Amazon", "opt44"),
						new Service(9, "", "AOL", "opt10"),
						new Service(10, "", "Apple", "opt131"),
						new Service(11, "", "autocosmos.com", "opt143"),
						new Service(12, "", "Avito", "opt59"),
						new Service(13, "", "Badoo", "opt56"),
						new Service(14, "", "BANDUS", "opt209"),
						new Service(15, "", "Bazos.sk", "opt138"),
						new Service(16, "", "Beget.com", "opt187"),
						new Service(17, "", "bet365", "opt17"),
						new Service(18, "", "Betano (+BETANO.ro)", "opt192"),
						new Service(19, "", "BetFair", "opt25"),
						new Service(20, "", "Betmgm", "opt223"),
						new Service(21, "", "Bitpanda", "opt237"),
						new Service(22, "", "Blizzard", "opt78"),
						new Service(23, "", "blsspain-russia.com", "opt135"),
						new Service(24, "", "Bolt", "opt81"),
						new Service(25, "", "Brevo", "opt217"),
						new Service(26, "", "bumble", "opt145"),
						new Service(27, "", "bunq", "opt199"),
						new Service(28, "", "bwin", "opt137"),
						new Service(29, "", "Careem", "opt89"),
						new Service(30, "", "casa.it", "opt148"),
						new Service(31, "", "Cash App", "opt226"),
						new Service(32, "", "Cashrewards", "opt214"),
						new Service(33, "", "Casino Plus", "opt201"),
						new Service(34, "", "ChoTot", "opt176"),
						new Service(35, "", "CityMobil", "opt76"),
						new Service(36, "", "Claude (Anthropic)", "opt196"),
						new Service(37, "", "Clubhouse", "opt98"),
						new Service(38, "", "CoinBase", "opt112"),
						new Service(39, "", "CONTACT", "opt51"),
						new Service(40, "", "Craigslist", "opt26"),
						new Service(41, "", "Credit Karma", "opt124"),
						new Service(42, "", "CupidMedia", "opt157"),
						new Service(43, "", "Czech email services", "opt150"),
						new Service(44, "", "Deliveroo", "opt53"),
						new Service(45, "", "DenimApp", "opt204"),
						new Service(46, "", "DiDi", "opt92"),
						new Service(47, "", "Discord", "opt45"),
						new Service(48, "", "DistroKid", "opt232"),
						new Service(49, "", "Dodopizza + PapaJohns", "opt27"),
						new Service(50, "", "Doordash", "opt40"),
						new Service(51, "", "Drom.RU", "opt32"),
						new Service(52, "", "Drug Vokrug", "opt31"),
						new Service(53, "", "dundle", "opt136"),
						new Service(54, "", "EasyPay", "opt21"),
						new Service(55, "", "ENEBA", "opt200"),
						new Service(56, "", "EUROBET", "opt141"),
						new Service(57, "", "Facebook", "opt2"),
						new Service(58, "", "FastMail", "opt43"),
						new Service(59, "", "Fbet", "opt215"),
						new Service(60, "", "Feeld", "opt159"),
						new Service(61, "", "Fiverr", "opt6"),
						new Service(62, "", "fontbet", "opt139"),
						new Service(63, "", "foodora", "opt189"),
						new Service(64, "", "foodpanda", "opt115"),
						new Service(65, "", "Fortuna", "opt221"),
						new Service(66, "", "Fotostrana", "opt13"),
						new Service(67, "", "funpay", "opt142"),
						new Service(68, "", "G2A.COM", "opt68"),
						new Service(69, "", "Gameflip", "opt77"),
						new Service(70, "", "Gamers set (offgamers.com, G2A.com, seagm.com)", "opt28"),
						new Service(71, "", "GetsBet.ro", "opt179"),
						new Service(72, "", "GetTaxi", "opt35"),
						new Service(73, "", "GGbet", "opt188"),
						new Service(74, "", "GGPokerUK", "opt229"),
						new Service(75, "", "giocodigitale.it", "opt85"),
						new Service(76, "", "Glovo & Raketa", "opt108"),
						new Service(77, "", "goldbet.it", "opt240"),
						new Service(78, "", "Google (YouTube, Gmail)", "opt1"),
						new Service(79, "", "Google Voice", "opt140"),
						new Service(80, "", "GrabTaxi", "opt30"),
						new Service(81, "", "Grailed", "opt420"),
						new Service(82, "", "Grindr", "opt110"),
						new Service(83, "", "Happn", "opt155"),
						new Service(84, "", "HelloTalk", "opt203"),
						new Service(85, "", "hepsiburada", "opt238"),
						new Service(86, "", "Hey", "opt216"),
						new Service(87, "", "Hinge", "opt120"),
						new Service(88, "", "hopper", "opt144"),
						new Service(89, "", "HUAWEI", "opt166"),
						new Service(90, "", "ICard", "opt103"),
						new Service(91, "", "idealista.com", "opt165"),
						new Service(92, "", "ifood", "opt55"),
						new Service(93, "", "IMO", "opt111"),
						new Service(94, "", "inbox.lv", "opt167"),
						new Service(95, "", "Inboxdollars", "opt118"),
						new Service(96, "", "Instagram (+Threads)", "opt16"),
						new Service(97, "", "Ipsos", "opt193"),
						new Service(98, "", "IQOS", "opt243"),
						new Service(99, "", "JD.com", "opt94"),
						new Service(100, "", "KakaoTalk", "opt71"),
						new Service(101, "", "Klarna", "opt175"),
						new Service(102, "", "kleinanzeigen.de", "opt152"),
						new Service(103, "", "KoronaPay", "opt99"),
						new Service(104, "", "Kuper (SberMarket)", "opt97"),
						new Service(105, "", "kwiff.com", "opt129"),
						new Service(106, "", "Lajumate.ro", "opt195"),
						new Service(107, "", "Lalamove", "opt180"),
						new Service(108, "", "LAPOSTE", "opt182"),
						new Service(109, "", "LASVEGAS.RO", "opt222"),
						new Service(110, "", "Lazada", "opt60"),
						new Service(111, "", "Leboncoin", "opt164"),
						new Service(112, "", "Line Messenger", "opt37"),
						new Service(113, "", "LinkedIn", "opt8"),
						new Service(114, "", "LiveScore", "opt42"),
						new Service(115, "", "LocalBitcoins", "opt105"),
						new Service(116, "", "Locanto.com", "opt114"),
						new Service(117, "", "Lyft", "opt75"),
						new Service(118, "", "Magnit", "opt126"),
						new Service(119, "", "Mail.RU", "opt33"),
						new Service(120, "", "Mail.ru Group", "opt4"),
						new Service(121, "", "Mamba", "opt100"),
						new Service(122, "", "Marktplaats", "opt171"),
						new Service(123, "", "maxline.by", "opt219"),
						new Service(124, "", "MiChat", "opt96"),
						new Service(125, "", "Microsoft (Azure, Bing, Skype, etc)", "opt15"),
						new Service(126, "", "mobileDE", "opt156"),
						new Service(127, "", "MOMO", "opt184"),
						new Service(128, "", "Monese", "opt121"),
						new Service(129, "", "MoneyLion", "opt47"),
						new Service(130, "", "MPSellers", "opt197"),
						new Service(131, "", "MrGreen", "opt211"),
						new Service(132, "", "MS Office 365", "opt7"),
						new Service(133, "", "myopinions & erewards", "opt0"),
						new Service(134, "", "Naver", "opt73"),
						new Service(135, "", "Nectar", "opt198"),
						new Service(136, "", "NetBet", "opt95"),
						new Service(137, "", "Neteller", "opt116"),
						new Service(138, "", "Netflix", "opt101"),
						new Service(139, "", "NHNCloud", "opt202"),
						new Service(140, "", "NHNcorp (강남언니)", "opt177"),
						new Service(141, "", "Nico", "opt119"),
						new Service(142, "", "novibet.com", "opt151"),
						new Service(143, "", "OD", "opt5"),
						new Service(144, "", "OfferUp", "opt113"),
						new Service(145, "", "OkCupid", "opt230"),
						new Service(146, "", "OKX", "opt228"),
						new Service(147, "", "OLX + goods.ru", "opt70"),
						new Service(148, "", "onet.pl (Onet Konto)", "opt241"),
						new Service(149, "", "OTHER (no guarantee)", "opt19"),
						new Service(150, "", "OTHER (voice code)", "opt00019"),
						new Service(151, "", "OurTime", "opt212"),
						new Service(152, "", "OZON.ru", "opt181"),
						new Service(153, "", "Paddy Power", "opt109"),
						new Service(154, "", "Pari.ru", "opt169"),
						new Service(155, "", "Parimatch", "opt3"),
						new Service(156, "", "Payoneer", "opt162"),
						new Service(157, "", "PayPal + Ebay", "opt83"),
						new Service(158, "", "Paysafecard", "opt122"),
						new Service(159, "", "PAYSEND", "opt183"),
						new Service(160, "", "pm.by", "opt149"),
						new Service(161, "", "POF.com", "opt84"),
						new Service(162, "", "Prom.UA", "opt107"),
						new Service(163, "", "Proton Mail", "opt57"),
						new Service(164, "", "Publi24", "opt207"),
						new Service(165, "", "Qiwi", "opt18"),
						new Service(166, "", "Rambler.ru", "opt154"),
						new Service(167, "", "Revolut", "opt133"),
						new Service(168, "", "ROOMSTER", "opt153"),
						new Service(169, "", "Royal Canin", "opt170"),
						new Service(170, "", "RusDate", "opt186"),
						new Service(171, "", "Samokat", "opt185"),
						new Service(172, "", "Samsung", "opt174"),
						new Service(173, "", "Schibsted-konto", "opt134"),
						new Service(174, "", "Shopee", "opt48"),
						new Service(175, "", "Signal", "opt127"),
						new Service(176, "", "Sisal", "opt38"),
						new Service(177, "", "Skout", "opt49"),
						new Service(178, "", "Skrill", "opt117"),
						new Service(179, "", "Snapchat", "opt90"),
						new Service(180, "", "SNKRDUNK", "opt190"),
						new Service(181, "", "Solitaire Cash", "opt234"),
						new Service(182, "", "Steam", "opt58"),
						new Service(183, "", "subito.it", "opt146"),
						new Service(184, "", "Swagbucks", "opt125"),
						new Service(185, "", "Tango", "opt82"),
						new Service(186, "", "TANK.RU", "opt161"),
						new Service(187, "", "Taptap", "opt239"),
						new Service(188, "", "Taxi Maksim", "opt74"),
						new Service(189, "", "Telegram", "opt29"),
						new Service(190, "", "Telegram (voice code)", "opt00029"),
						new Service(191, "", "Tencent QQ", "opt34"),
						new Service(192, "", "Ticketmaster", "opt52"),
						new Service(193, "", "TikTok", "opt104"),
						new Service(194, "", "Tinder", "opt9"),
						new Service(195, "", "TLScontact", "opt235"),
						new Service(196, "", "TopCashback", "opt191"),
						new Service(197, "", "TOTOGAMING", "opt220"),
						new Service(198, "", "TransferGo", "opt218"),
						new Service(199, "", "TrueCaller", "opt233"),
						new Service(200, "", "Truth Social", "opt244"),
						new Service(201, "", "Twilio", "opt66"),
						new Service(202, "", "Twitch", "opt205"),
						new Service(203, "", "U By Prodia", "opt160"),
						new Service(204, "", "Uber", "opt72"),
						new Service(205, "", "Verse", "opt39"),
						new Service(206, "", "Viber", "opt11"),
						new Service(207, "", "Vinted", "opt130"),
						new Service(208, "", "VK", "opt69"),
						new Service(209, "", "VonageVF", "opt178"),
						new Service(210, "", "VooV Meeting", "opt147"),
						new Service(211, "", "Waitomo", "opt213"),
						new Service(212, "", "WalletHub", "opt206"),
						new Service(213, "", "Walmart", "opt227"),
						new Service(214, "", "WEB.DE", "opt172"),
						new Service(215, "", "WebMoney&ENUM", "opt24"),
						new Service(216, "", "WeChat", "opt67"),
						new Service(217, "", "Weebly", "opt54"),
						new Service(218, "", "WESTSTEIN", "opt80"),
						new Service(219, "", "Whatnot", "opt231"),
						new Service(220, "", "WhatsAPP", "opt20"),
						new Service(221, "", "WhatsAPP (voice code)", "opt00020"),
						new Service(222, "", "Whoosh", "opt123"),
						new Service(223, "", "Wing Money", "opt106"),
						new Service(224, "", "Wise", "opt91"),
						new Service(225, "", "Wolt", "opt163"),
						new Service(226, "", "WooPlus", "opt208"),
						new Service(227, "", "X (Twitter)", "opt41"),
						new Service(228, "", "X World Wallet", "opt173"),
						new Service(229, "", "Yahoo", "opt65"),
						new Service(230, "", "Yalla.live", "opt88"),
						new Service(231, "", "Yandex&YooMoney", "opt23"),
						new Service(232, "", "Year13", "opt236"),
						new Service(233, "", "Zalo", "opt158"),
						new Service(234, "", "Zasilkovna", "opt225"),
						new Service(235, "", "Zoho", "opt93"),
						new Service(236, "", "ZoomInfo", "opt194")
		]) {
	}
	public static SMSPVAPI Instance { get; } = new SMSPVAPI();
}

