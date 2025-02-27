using static Chameleon.lib.Abs.Platformatic.Client;

namespace Chameleon.lib.Abs.Platformatic;
public class Plair {
  readonly string prefix = "/plair";
  readonly string[] backgrounds = ["witty-sarcastic", "casual-relatable", "informative-but-funny", "straightforward-critical"];
  Plair() { }

  public record AskRequest(string Featue, object Scenario, string? Background = null);
  public record AskResponse(string Response);
  public async Task<Response<AskResponse>?> Ask(AskRequest request) {
    return await Client.Instance.Post<Response<AskResponse>>($"{prefix}/ask/{request.Featue}", new() {
      Q = $"?background={Uri.EscapeDataString(request.Background ?? backgrounds[new Random().Next(0, backgrounds.Length)])}",
      Body = new { scenario = request.Scenario },
    });
  }

  // singleton
  public static Plair Instance { get; } = new();
}
