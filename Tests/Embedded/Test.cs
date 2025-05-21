using System.Diagnostics;
using chameleon.assets;
using Chameleon.lib.Const;

namespace Tests.Embedded;

public class Test
{
	[Fact]
	public async Task Embedded_Loader_Copy() {
		// Arrange
		var file = "js.node.node";
		var target = "/Users/dev/src/Chameleon-lib/Tests/Embedded/node";
		var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		//var settings = "{}";

		Debug.WriteLine($"Testing LoadExtension with destination path: {destinationPath}");
		try {
			// Act
			var success = await Load.Copy(file, target);
			Assert.True(success, $"Failed to copy {file} to {target}");

		} finally {
			// Clean up
			File.Delete(target);
		}
	}

}
