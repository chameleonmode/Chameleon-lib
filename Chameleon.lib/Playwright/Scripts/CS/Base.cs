using Microsoft.Playwright;
namespace Chameleon.lib.Playwright.Scripts.CS;
public abstract class Base {
  public virtual async Task<IPage> NewPage(
    IBrowserContext context,
    int avigationTimeout = 1000 * 60 * 2, int timeout = 1000 * 60 * 5
  ) {
    var page = await context.NewPageAsync();
    page.SetDefaultNavigationTimeout(1000 * 60 * 2); // 2 minutes
    page.SetDefaultTimeout(1000 * 60 * 5); // 5 minutes
    return page;
  }
}
