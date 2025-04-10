namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;

public class Google : JSScript {
  public Google() : base(
    "reddit/plugins/login/google",
    "Reddit Google Authentication",
    "Authenticate to reddit using google account"
  ) { }

  public override IDictionary<string, string> Parameters => new Dictionary<string, string> {
    { "username", "Google username" },
    { "password", "Google password" }
  };
}