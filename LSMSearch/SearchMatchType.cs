using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LSMSearch;

[JsonConverter(typeof(StringEnumConverter))]
public enum SearchMatchType
{
	PartialMatch,
	FullMatch,
	TransitiveMatch
}