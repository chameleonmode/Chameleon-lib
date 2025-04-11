using Chameleon.lib.Playwright.HtmlProcessingPipeline.Models;
using Microsoft.Playwright;
using System.Text;
using System.Text.RegularExpressions;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.Extensions;
public static partial class PipelineExtensions {

	private static readonly Regex ClickRegex = ClickRegexMethod();

	private static readonly Regex FillRegex = FillRegexMethod();

	public static async Task PerformPartialScriptActions(this IPage page, string partialScript) {

		var clickMatches = ClickRegex.Matches(partialScript);
		foreach (Match m in clickMatches) {
			var directLocatorSelector = m.Groups[3].Value; 
			var nestedLocatorSelector = m.Groups[2].Value;  

			var selectorToClick = !string.IsNullOrEmpty(nestedLocatorSelector)
					? nestedLocatorSelector
					: directLocatorSelector;

			if (!string.IsNullOrWhiteSpace(selectorToClick)) {
				Console.WriteLine($"Performing click on selector: {selectorToClick}");
				await page.ClickAsync(selectorToClick);
			}
		}

		var fillMatches = FillRegex.Matches(partialScript);
		foreach (Match m in fillMatches) {

			var selector = m.Groups[2].Value;
			var textToFill = m.Groups[4].Value;
			if (!string.IsNullOrWhiteSpace(selector)) {
				Console.WriteLine($"Performing fill on selector: {selector}, text: {textToFill}");
				await page.FillAsync(selector, textToFill);
			}
		}

		//More calls if needed
	}

	[GeneratedRegex(@"await\s+page(?:\.locator\((['""])(.*?)\1\))?\.click\((?:['""](.*?)['""])?\)", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
	private static partial Regex ClickRegexMethod();
	[GeneratedRegex(@"await\s+page\.fill\(\s*(['""])(.*?)\1\s*,\s*(['""])(.*?)\3\s*\)", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
	private static partial Regex FillRegexMethod();

	public static string BuildMultiStepDescription(this IEnumerable<StepDefinition> steps) {
		var sb = new StringBuilder();

		sb.AppendLine("We have multiple steps to accomplish. Please produce one JavaScript Playwright script that does all these steps in order, with structured logging and error handling.");

		var i = 1;
		foreach (var step in steps) {
			sb.AppendLine($"Step {i}: {step.Description}");
			i++;
		}

		sb.AppendLine();
		sb.AppendLine("Important requirements:");
		sb.AppendLine("1) Use only 'import' statements (no require).");
		sb.AppendLine("2) Use dynamic or robust selectors if possible.");
		sb.AppendLine("3) Include console.log statements before/after major actions.");
		sb.AppendLine("4) If environment variables or options are needed, reference them or show how to pass them in.");

		return sb.ToString();
	}

}