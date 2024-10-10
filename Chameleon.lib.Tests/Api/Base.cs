using System.Diagnostics;
using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class Base {
	internal readonly string email = "elimdadia@gmail.com", lkey = "HHTQ-QJYS-ZMWX-CO5U";
	public Base()
	{
		HttpApiClient.Instance.OnAuthError += async() => {
			var login = await Auther.LoginAsync(email, lkey);
			Assert.NotNull(login.AccessToken);
			Assert.NotNull(login.RefreshToken);

			var refresh = await Auther.RefreshTokenAsync(login.AccessToken, login.RefreshToken);
			Debug.WriteLine("Auth error");
		};
		HttpApiClient.Instance.OnRetry += (ex) => {
			Debug.WriteLine(ex);
		};
	}
}
