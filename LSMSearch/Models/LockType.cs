using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LSMSearch.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum LockType
{
	Cylinder,
	SmartHandle,
}