using System.Diagnostics;

using Chameleon.lib.Api;

namespace Tests.APiv1;
public class ApiTestsBase {
	public TaskCompletionSource tcs = new();
	public LoginResponse? LoginResponse { get; set; }
	public ApiTestsBase() {
		Login();
		HttpApiClient.Instance.OnAuthError += () => {
			Debug.WriteLine("OnAuthError");
			return Task.CompletedTask;
		};
		HttpApiClient.Instance.OnRetry += (ex) => {
			Debug.WriteLine(ex);
		};
	}

	private async void Login() {
		await Auther.LoginAsync(TestEnvironment.Directory[1].email, TestEnvironment.Directory[1].license);
		LoginResponse = Auther.AuthSession;
		_ = tcs.TrySetResult();
	}
}
