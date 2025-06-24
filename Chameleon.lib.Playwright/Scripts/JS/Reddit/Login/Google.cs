using Chameleon.lib.AIR.Scripts;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public record Google : JSScript {
  public Google() : base(
    "../scripts/reddit/plugins/login/default/google",
    "Reddit Login With Google",
    "Authenticate to reddit using google account"
  ) { }

  public override Dictionary<string, string> Args => new() {
    { "email", "email" },
    { "password", "password" },
    { "title", "Google" },
    { "website", "Google.com" }
  };
}