using System.Text.Json.Serialization;

namespace Chameleon.lib.Common.Models.Interfaces;
public class Dto : IHasid {
	public int id { get; set; }
	[JsonIgnore] public string ID => id.ToString();
	public string? title { get; set; }

	public string? Tags { get; set; }
}
