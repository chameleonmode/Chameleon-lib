namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public class Google : JSScript {
  public Google() : base(
    "reddit/plugins/login/google",
    "Reddit Login With Google",
    "Authenticate to reddit using google account"
  ) { }

  public override IDictionary<string, string> Parameters => new Dictionary<string, string> {
    { "email", "email" },
    { "password", "password" },
    { "title", "Google" },
    { "website", "Google.com" }
  };
}