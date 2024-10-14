using System.Diagnostics;
using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class ApiTestsBase {
	internal readonly string email = "elimdadia@gmail.com", lkey = "HHTQ-QJYS-ZMWX-CO5U";
	public TaskCompletionSource tcs = new();
	public LoginResponse? LoginResponse { get; set; }	
	public ApiTestsBase()
	{
		Login();
		HttpApiClient.Instance.OnAuthError +=() => {
			Debug.WriteLine("OnAuthError");
			return Task.CompletedTask;
		};
		HttpApiClient.Instance.OnRetry += (ex) => {
			Debug.WriteLine(ex);
		};
	}

	private async void Login()
	{
		LoginResponse = await Auther.LoginAsync(email, lkey);
		_ = tcs.TrySetResult();
	}
}
