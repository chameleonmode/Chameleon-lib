using System.Text.Json.Serialization;

namespace Chameleon.lib.Common.Models;
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
