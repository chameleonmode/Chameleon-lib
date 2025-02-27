using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;

public class Plair {
  readonly string prefix = "/plair";
  Plair() {}

  public record Response<T>(
    T Payload
  );

  public record AskResponsePayload(string Response);

  public async Task<Response<AskResponsePayload>?> Ask(string feature, string background, object scenario) {
    // await DB.Instance.EnsureUser();
    return await Client.Instance.Post<Response<AskResponsePayload>>($"{prefix}/ask/{feature}", new (){
      Q = $"?background={Uri.EscapeDataString(background)}",
      Body = new { scenario },
    });
	}

  // singleton
  public static Plair Instance { get; } = new();
}
