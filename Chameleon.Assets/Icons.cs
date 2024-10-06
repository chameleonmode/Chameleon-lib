using System.Text.Json;
using Chameleon.lib.Common.Constants;

using Chameleon.lib.Common.Models;

namespace chameleon.assets;
public class Icons {
	public static Icons Instance { get; } = new Icons();

	private readonly Lazy<Task<List<FontIconInfo>>> _fontIcons = new(LoadFontIcons);

	public Task<List<FontIconInfo>> FontIcons => _fontIcons.Value ?? Task.FromResult<List<FontIconInfo>>([]);

	private static async Task<List<FontIconInfo>> LoadFontIcons()
	{
		return await Task.Run(() => {
			using var s = Loader.Instance.Open(new Uri(Consts.Json.JsonFontsEmbeddedDir));
			var icons = JsonSerializer.Deserialize(s, Jsonz.Default.ListFontIconInfo);

			return icons ?? [];
		});
	}
}
