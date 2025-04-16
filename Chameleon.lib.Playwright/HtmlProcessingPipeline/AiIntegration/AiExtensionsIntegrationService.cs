using Chameleon.lib.Playwright.HtmlProcessingPipeline.HtmlExtraction;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using System.Text;

namespace Chameleon.lib.Playwright.HtmlProcessingPipeline.AiIntegration;
public class AiExtensionsIntegrationService {
	private readonly IChatClient chatClient;
	public AiIntegrationOptions Options { get; }

	public AiExtensionsIntegrationService(AiIntegrationOptions options) {
		this.Options = options ?? throw new ArgumentNullException(nameof(options));

		if (string.IsNullOrWhiteSpace(options.ApiKey))
			throw new ArgumentException("API key must be provided in options.", nameof(options));

		if (string.IsNullOrWhiteSpace(options.ModelName))
			throw new ArgumentException("Model name must be provided in options.", nameof(options));


		//var openAiClient = new OpenAIClient(options.ApiKey);
		//chatClient = openAiClient.AsChatClient(options.ModelName);
		chatClient = new GeminiChatClient(options.ApiKey, options.ModelName);
	}

	public async Task<string> QueryLLMAsync(string prompt, AiIntegrationOptions options, CancellationToken cancellationToken = default) {
		var messages = new List<ChatMessage>
						{
								new(ChatRole.System, "You are a helpful assistant generating automation scripts."),
								new(ChatRole.User, prompt)
						};
		var chatOptions = new ChatOptions {
			MaxOutputTokens = options.MaxTokens
		};
		if (options.Temperature is not null) {
			chatOptions.Temperature = options.Temperature.Value;
		}

		var response = await chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

		return response.Text;
	}

	public async Task<string> GenerateAutomationScriptAsync(IEnumerable<HtmlChildSummary> relevantNodes, string automationRequest) {
		var sb = new StringBuilder();
		sb.AppendLine("You are a helpful assistant that generates **valid JavaScript Playwright scripts** only.");
		sb.AppendLine("Any additional explanations or disclaimers must be wrapped in JavaScript comments so the resulting code is directly runnable with Node.js or any JavaScript environment.");
		sb.AppendLine();
		sb.AppendLine($"User wants to automate: {automationRequest}");
		sb.AppendLine();
		sb.AppendLine("Relevant DOM nodes:");
		foreach (var node in relevantNodes) {
			sb.AppendLine($"- Tag: {node.TagName}, ID: {node.Id}, Class: {node.ClassName}, Snippet: {node.ShortText}, Potential CssSelecotor:{node.CssSelector}");
		}

		sb.AppendLine();
		sb.AppendLine("Now, generate a JavaScript Playwright script that interacts with these elements.");

		sb.AppendLine("The entire script must be wrapped in:");
		sb.AppendLine();
		sb.AppendLine("export default async function (");
		sb.AppendLine("  context,");
		sb.AppendLine("  options: {");
		sb.AppendLine("	   // Here you can list any keys you need, for example:");
		sb.AppendLine("    // username;");
		sb.AppendLine("    // password;");
		sb.AppendLine("    // search;");
		sb.AppendLine("    // commentTitle;");
		sb.AppendLine("    // ...:;");
		sb.AppendLine("		}");
		sb.AppendLine("		) {");
		sb.AppendLine("		// ...");
		sb.AppendLine("	}");
		sb.AppendLine();
		sb.AppendLine("For playwright page create it from context parameter");
		sb.AppendLine("eg. const page = await context.newPage();");
		sb.AppendLine();
		sb.AppendLine("Requirements:");
		sb.AppendLine("1. The first parameter, `context`");
		sb.AppendLine("2.The second parameter, `options`, includes any relevant fields or parameters for the automation tasks.");
		sb.AppendLine("3.If you need environment variables, either:");
		sb.AppendLine("	 -read them directly inside the function(e.g., `const user = process.env.USERNAME`), or");
		sb.AppendLine("	 -assume they've been passed in as `options.username`, etc.");
		sb.AppendLine("4.The final output should be a **fully valid** JavaScript code(do not include any Typescript) snippet that can be directly run or imported by a test harness.");
		sb.AppendLine("5.Do **not* **produce** text outside of valid code.Any disclaimers or details should appear only in code comments.");
		sb.AppendLine();
		sb.AppendLine("Example usage:");
		sb.AppendLine("-The script might navigate to a site, fill out a login form, create or edit content, log out, etc.");
		sb.AppendLine("-You may add logs with `console.log()` or error handling with `try/catch`, but keep everything inside `export default async function(...) { ... }`.");
		sb.AppendLine();
		sb.AppendLine("Remember to return only the code block containing the exported function, with no extra lines before or after.");
		sb.AppendLine();
		sb.AppendLine("**Important**:");
		sb.AppendLine("- Put any disclaimers or usage notes in JavaScript comments at the top.");
		sb.AppendLine("- The resulting script must be purely valid JavaScript.");
		sb.AppendLine("");
		sb.AppendLine();
		sb.AppendLine("Now produce the **full** JavaScript code, do not include any Typescript.");

		var finalPrompt = sb.ToString();
		var script = await QueryLLMAsync(finalPrompt, Options, CancellationToken.None);
		return script;
	}

}
