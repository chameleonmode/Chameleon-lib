using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Chameleon.lib.Auth;

namespace Chameleon.lib.Abs;

#region Models / Dto's
public record User(
  object Id,
  string UserId,
  string Email,
  string? LicenseKey,
  string TenantId,
  string Provider,
  string? ProviderId,
  DateTime CreatedAt,
  DateTime UpdatedAt
);
public record DataInteraction(
  object Id,
  string InteractionId,
  string TenantId,
  string SenderId,
  string ReceiverId,
  string DataType,
  string DataPayload,
  DateTime CreatedAt
);
public record Tag(int Id, string Name, string Items, string TenantId);
public record ItemTag(string TagItemType, string TagItemId, string TagName, string TenantId);
public record Errorer(string Error, string Message);
public record Request(
  string? Q = null,
  object? Body = null,
  bool EnsureSuccess = true,
  bool Authenticate = true,
  HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead,
  Dictionary<string, string>? Headers = null
) {
  public HttpContent? Content => Body == null ? null : JsonContent.Create(Body, mediaType: null, JSON.InsensitiveCamelCaseOptions);
}
public abstract class Root(string prefix) {
  public string Prefix { get; } = '/' + prefix;
}

public abstract class Web {
  static Task<T?> Sender<T>(HttpMethod method, string path, Request? request = null) => Abs.Send<T>(method, path, request ?? new());
  public static Task<T?> Put<T>(string path, Request request) => Sender<T>(HttpMethod.Put, path, request);
  public static Task<T?> Post<T>(string path, Request request) => Sender<T>(HttpMethod.Post, path, request);
  public static Task<T?> Get<T>(string path, Request? request = null) => Sender<T>(HttpMethod.Get, path, request);
  public static Task<T?> Delete<T>(string path, Request? request = null) => Sender<T>(HttpMethod.Delete, path, request);
}
#endregion

public static class Abs {
  private static Task<bool>? testing;
  public static Task<bool> TESTING => testing ??= Task.Run(async () => {
    try {
      using var client = new HttpClient();
      client.Timeout = TimeSpan.FromMilliseconds(300);

      _ = await client.GetAsync("http://127.0.0.1:3042");
      return true && Debugger.IsAttached; // Local server is available
    } catch {
      return false; // Use fallback
    }
  });

  public static async Task<HttpClient> HttpClient() => new HttpClient(new HttpClientHandler {
    AutomaticDecompression = DecompressionMethods.GZip,
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
  }) { BaseAddress = new Uri(await TESTING ? "http://127.0.0.1:3042" : "https://chameleon-ws.onrender.com") };

  public static async Task<T?> Send<T>(HttpMethod method, string path, Request req) {
    using var client = await HttpClient();
    if (req.Authenticate) {
      var (auth0client, authentication) = await Session.Instance.Authenticate();
      client.DefaultRequestHeaders.Authorization = authentication;
      client.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");
    }
    foreach (var header in req.Headers ?? []) client.DefaultRequestHeaders.Add(header.Key, header.Value);

    var requestUri = $"{path}{req.Q ?? ""}";
    using var response = await client.SendAsync(new HttpRequestMessage(method, requestUri) {
      Content = req.Content
    }, req.CompletionOption);

    if (req.CompletionOption == HttpCompletionOption.ResponseHeadersRead) {
      _ = response.EnsureSuccessStatusCode();
      return typeof(T) == typeof(HttpResponseMessage) ? (T)(object)await response.Content.ReadAsStreamAsync() : default;
    }

    var content = await response.Content.ReadAsStringAsync();
    return response.IsSuccessStatusCode || !req.EnsureSuccess ? JSON.Deserialize<T>(content) : throw new HttpRequestException(
      $"{requestUri}:\n{response.StatusCode}\n{(JSON.Deserialize<Errorer>(content) is Errorer err ? $"{err.Error}\n{err.Message}" : content)}");
  }

}

