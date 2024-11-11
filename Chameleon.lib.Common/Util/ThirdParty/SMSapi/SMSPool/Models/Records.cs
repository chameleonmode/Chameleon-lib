using Chameleon.lib.Common.Records;

namespace Chameleon.lib.Common.Util.ThirdParty.SMSapi.SMSPool.Models;
public record Country(int ID, string Name, string Short_name, string Region) : RCountry(Name);
public record Service(int ID, string Name, int Favourite) : RService(Name);
