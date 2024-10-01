using System;
using System.Diagnostics;

using chameleon.assets;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Types;
using Chameleon.lib.WebBrowser.Interfaces;
using Chameleon.lib.WebBrowser.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using static Chameleon.lib.Common.Constants.Enums;

namespace Chameleon.lib.Tests.WebBrowser;
public class ExtensionLoaderServiceTests : WebBroswserTestsBase {
	[Fact]
	public async Task LoadExtension_ValidExtension_Succeeds()
	{
		_ = await _tcs.Task;
		Assert.NotNull(ExtensionLoaderService);

		// Arrange
		var extensionType = ExtensionType.chromeleon;
		var destinationPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var settings = "{}";

		Debug.WriteLine($"Testing LoadExtension with destination path: {destinationPath}");
		try {
			// Check if the resource exists
			var resourceNames = typeof(Constas).Assembly.GetManifestResourceNames();
			foreach (var name in resourceNames) {
				Debug.WriteLine(name);
			}

			// Act
			await ExtensionLoaderService!.LoadExtension(extensionType, destinationPath, settings);

			// Assert
			var dest = Path.Combine(destinationPath, extensionType.ToString());
			Assert.True(Directory.Exists(dest), $"Destination path does not exist: {dest}");

			var manifestPath = Path.Combine(dest, "manifest.json");
			Assert.True(File.Exists(manifestPath), $"Manifest file does not exist: {manifestPath}");

			var iconsPath = Path.Combine(dest, "data", "icons");
			Assert.True(Directory.Exists(iconsPath), $"Icons directory does not exist: {iconsPath}");

		} finally {
			// Clean up
			if (Directory.Exists(destinationPath)) {
				Directory.Delete(destinationPath, true);
			}
		}
	}
}
