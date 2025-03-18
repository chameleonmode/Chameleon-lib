namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public interface IAiIntegrationService {
	Task<string> GenerateScriptAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default);
}
