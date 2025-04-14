namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public class AiIntegrationOptions {
	public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

	public string? ModelName { get; set; }

	public string ApiKey { get; set; } = "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";

	public float? Temperature { get; set; }

	public int MaxTokens { get; set; } = 150;
}
