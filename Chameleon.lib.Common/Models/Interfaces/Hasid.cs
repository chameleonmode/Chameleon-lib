namespace Chameleon.lib.Common.Models.Interfaces;
public class Dto : IHasid {
	public int id { get; set; }
	public string? title { get; set; }

	public string? Tags { get; set; }
}
