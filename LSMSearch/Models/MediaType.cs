using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LSMSearch.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum MediaType
{
	Card,
	TransponderWithCardInlay,
	Transponder
}