using Chameleon.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public record Google : JSScript {
  public Google() : base(
    "../scripts/reddit/plugins/login/google",
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