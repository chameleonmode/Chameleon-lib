using System.Diagnostics;
using System.Linq.Expressions;
using chameleon.assets;
using Chameleon.lib.Const;

namespace Tests.Embedded;

public class Test {
	[Fact]
	public async Task Embedded_Loader_Copy() {
		// Arrange
		var file = "js.node.node";
		var target = "/Users/dev/src/Chameleon-lib/Tests/Embedded/node";
		//var settings = "{}";

		Debug.WriteLine($"Testing LoadExtension with destination path: {file}");
		try {
			// Act
			var success = await Resources.Copy(file, target);
			Assert.True(success, $"Failed to copy {file} to {target}");

		} finally {
			// Clean up
			File.Delete(target);
		}
	}
	[Fact]
	public async Task Embedded_Loader_Resource() {
		var source = "plugins.playwright";
		var target = "/Users/dev/src/Chameleon-lib/Tests/Embedded/cache";
		try {
			// Act
			var success = await Resources.Mapped(source, target);
			Assert.True(success, $"Failed to copy {source} to {target}");
		} catch (Exception e) {
			Debug.WriteLine(e);
		} finally {
			// Clean up
			Directory.Delete(target, true);
		}
	}

		[Fact]
	public async Task Resources_Dir() {
		var source = "plugins.playwright";
		var target = "/Users/dev/src/Chameleon-lib/Tests/Embedded/cache";
		try {
			// Act
			var success = await Resources.Mapped(source, target);
			Assert.True(success, $"Failed to copy {source} to {target}");
		} catch (Exception e) {
			Debug.WriteLine(e);
		} finally {
			// Clean up
			Directory.Delete(target, true);
		}
	}
}
