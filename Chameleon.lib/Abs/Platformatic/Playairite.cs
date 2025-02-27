using Chameleon.lib.Const;

namespace Chameleon.lib.Abs.Platformatic;

public class Playairite {
  Playairite() {}

  public record Response<T>(
    T Payload
  );

  public async Task<Response<object>?> Ask(string feature, string background) {
		await DB.Instance.EnsureUser();
		return await Client.Instance.Get<Response<object>>(Configs.Endpoints.DataInteractions, new (){
      Q = $"?feature={Uri.EscapeDataString(feature)}",
      Body = new { background },
    });
	}
}
