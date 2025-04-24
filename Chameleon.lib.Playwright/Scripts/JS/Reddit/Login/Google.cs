using Chameleon.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public class Google : JSScript {
  public Google() : base(
    "reddit/plugins/login/google",
    "Reddit Login With Google",
    "Authenticate to reddit using google account"
  ) { }

  public override Dictionary<string, string> Parameters => new() {
    { "email", "email" },
    { "password", "password" },
    { "title", "Google" },
    { "website", "Google.com" }
  };
}