using System.Text.Json.Serialization;

using Chameleon.lib.Common.Models;

namespace Chameleon.lib.Common.Constants;
[JsonSerializable(typeof(List<FontIconInfo>))]
public partial class Jsonz : JsonSerializerContext {
}
