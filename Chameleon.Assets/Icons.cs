using System.Text.Json;
using System.Text.Json.Serialization;

namespace chameleon.assets;
public class FontIconInfo {
	[JsonConstructor]
	public FontIconInfo(string name, string codepoint)
	{
		Name = name;
		Codepoint = codepoint;
		XamlGlyph = $"&#x{codepoint};";
		CSharpGlyph = $"\\u{codepoint}";

		XamlExample = @"<ui:FontIcon Glyph=""" + XamlGlyph + @""" />";
		CSharpExample = @"FontIcon fontIcon = new FontIcon()" + Environment.NewLine +
				@"fontIcon.Glyph = """ + CSharpGlyph + @""";";

		Glyph = char.ConvertFromUtf32((int)Convert.ToUInt32(codepoint, 16)).ToString();
	}

	public string Name { get; set; }

	public string Codepoint { get; set; }

	public string Glyph { get; }

	[JsonIgnore]
	public string XamlGlyph { get; }

	[JsonIgnore]
	public string CSharpGlyph { get; }

	[JsonIgnore]
	public string XamlExample { get; }

	[JsonIgnore]
	public string CSharpExample { get; }
}

[JsonSerializable(typeof(List<FontIconInfo>))]
public partial class Jsonz : JsonSerializerContext {
}

public class Icons {
	public const string JsonFontsEmbeddedDir = "chameleon.assets.json.fa_symbolfonts.json";

	public static Icons Instance { get; } = new Icons();

	private readonly Lazy<Task<List<FontIconInfo>>> _fontIcons = new(LoadFontIcons);

	public Task<List<FontIconInfo>> FontIcons => _fontIcons.Value ?? Task.FromResult<List<FontIconInfo>>([]);

	private static async Task<List<FontIconInfo>> LoadFontIcons()
	{
		return await Task.Run(() => {
			using var s = Loader.Instance.Open(JsonFontsEmbeddedDir);
			var icons = JsonSerializer.Deserialize(s, Jsonz.Default.ListFontIconInfo);

			return icons ?? [];
		});
	}
}
