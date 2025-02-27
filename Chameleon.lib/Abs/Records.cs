
using System.Net.Http.Json;
using Chameleon.lib.Const;

namespace Chameleon.lib.Abs;

public record Params(
  string? Q = null,
  object? Body = null,
  bool EnsureSuccess = true,
  bool Authorize = true,
  HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead
) {
  public HttpContent? Content => Body == null ? null
    : JsonContent.Create(Body, mediaType: null, JS.InsensitiveCamelCaseOptions);
}
