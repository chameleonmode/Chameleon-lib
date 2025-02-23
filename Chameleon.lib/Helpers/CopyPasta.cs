
using Chameleon.lib.Interfaces.Services;

namespace Chameleon.lib.Helpers;
public class CopyPasta {
  public ICopyPastaService? CopyPastaService { get; } = IoC.GetService<ICopyPastaService>();
  private CopyPasta() { }
  public static async Task Copy(string text) {
    if (Instance.CopyPastaService != null) {
      await Instance.CopyPastaService.SetTextAsync(text);
    }
  }
  public static CopyPasta Instance { get; } = new CopyPasta();
}
