using Chameleon.lib.Common.Records;

namespace Chameleon.lib.Common.Util.ThirdParty.SMSapi.SMSPVA.Models;
public record class Service(int ID, string Logo, string Name, string Code) : RService(Name);
public record class Country(int ID, string Name, string Code) : RCountry(Name);