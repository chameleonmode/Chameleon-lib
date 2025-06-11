using Chameleon.lib.Common.Models.Dto;

namespace Chameleon.lib.Api.Repos;

public class BrowserSettingsRepo : ApiBase<BrowserSettingDto> {
	private BrowserSettingsRepo() : base(Consts.Api.Endpoints.BrowserSettings) { }

	public static BrowserSettingsRepo Instance { get; } = new BrowserSettingsRepo();
}

