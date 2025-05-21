using System.Diagnostics;
using chameleon.assets;

namespace Tests.Embedded;

public class Test {
	[Fact]
	public async Task Resources_Copy() {
		// Arrange
		var target = "/Users/dev/src/Chameleon-lib/Tests/Embedded/node";
		var node = "node" + (OperatingSystem.IsWindows() ? ".exe" : "");
		var file = Path.Combine(target, node);
		try {
			// Act
			var success =
			await Resources.Copy("js.node." + node, file);
			Assert.True(success, $"Failed to copy {node} to {target}");
		} finally {
			// Clean up
			File.Delete(file);
		}
	}
	[Fact]
	public async Task Resources_Mapped() {
		var source = "plugins";
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
			var success = await Resources.Dir(source, target);
			Assert.True(success, $"Failed to copy {source} to {target}");
		} catch (Exception e) {
			Debug.WriteLine(e);
		} finally {
			// Clean up
			Directory.Delete(target, true);
		}
	}
}
