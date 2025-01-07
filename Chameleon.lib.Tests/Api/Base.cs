using System.Diagnostics;
using Chameleon.lib.Api;

namespace Chameleon.lib.Tests.Api;
public class ApiTestsBase {
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
		await Auther.LoginAsync(lib.Tests.Api.Environment.Directory[1].email, lib.Tests.Api.Environment.Directory[1].license);
		LoginResponse = Auther.AuthSession;
		_ = tcs.TrySetResult();
	}
}
