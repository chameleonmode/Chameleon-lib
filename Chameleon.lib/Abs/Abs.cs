using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Chameleon.lib.Auth;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs;

#region Models / Dto's
public record Tag(int Id, string Name, string Items, string TenantId);
public record ItemTag(string TagItemType, string TagItemId, string TagName, string TenantId);

/// Platformatic {\"statusCode\":400,\"code\":\"DB_USER\",\"error\":\"Bad Request\",\"message\":\"user exists\"}
public record Errorer(int? StatusCode, string? Code, string? Error, string? Message);
public record Request(string? Path = null,
  string? Q = null,
  object? Body = null,
  bool EnsureSuccess = true,
  bool Authenticate = true,
  HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead,
  Dictionary<string, string>? Headers = null,
  HttpMethod? Method = null
) {
  public string Uri => $"{Path}{Q ?? ""}";
  public HttpContent? Content => Body == null ? null : JsonContent.Create(Body, mediaType: null, JSON.InsensitiveCamelCaseOptions);
  public HttpRequestMessage RequestMessage => new(Method ?? throw new ArgumentException("Request method cannot be null."), Uri) {
    Content = Content
  };
}

public abstract class Root(string prefix) {
  public string Prefix { get; } = '/' + prefix;
}
public abstract class Web {
  static Task<T?> Sender<T>(HttpMethod method, Request request) => Abs.Send<T>(request with { Method = method });
  public static Task<T?> Put<T>(Request request) => Sender<T>(HttpMethod.Put, request);
  public static Task<T?> Post<T>(Request request) => Sender<T>(HttpMethod.Post, request);
  public static Task<T?> Get<T>(Request request) => Sender<T>(HttpMethod.Get, request);
  public static Task<T?> Delete<T>(Request request) => Sender<T>(HttpMethod.Delete, request);
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
  }) {
    BaseAddress = new Uri(
    await TESTING
       ? "http://127.0.0.1:3042"
       : "https://chameleon-ws.onrender.com"
  )
  };

  public static async Task<T?> Send<T>(Request req) {
    using var client = await HttpClient();
    if (req.Authenticate) {
      var (auth0client, authentication) = await Session.I.Authenticate();
      client.DefaultRequestHeaders.Authorization = authentication;
      client.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");
    }
    foreach (var header in req.Headers ?? []) client.DefaultRequestHeaders.Add(header.Key, header.Value);

    using var response = await client.SendAsync(req.RequestMessage, req.CompletionOption);

    if (req.CompletionOption == HttpCompletionOption.ResponseHeadersRead) {
      _ = response.EnsureSuccessStatusCode();
      return typeof(T) == typeof(HttpResponseMessage) ? (T)(object)await response.Content.ReadAsStreamAsync() : default;
    }

    var content = await response.Content.ReadAsStringAsync();
    return response.IsSuccessStatusCode || !req.EnsureSuccess ? JSON.Deserialize<T>(content)
    : JSON.Deserialize<Errorer>(content) is Errorer err ? throw new Exception($"{req.Uri}: {err.Error} {err.Message}")
      : throw new HttpRequestException($"{req.Uri}: {response.StatusCode} {content}");
  }
  // public static async Task<T> Send<T>(Request req) => await Send<T>(
  //   req.Method ?? throw new ArgumentException("Request method cannot be null."),
  //   req
  // ) ?? throw new InvalidOperationException("Failed to send request.");
}

