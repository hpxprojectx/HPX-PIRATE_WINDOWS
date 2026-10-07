using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPXPirate.Services;

public static class JsonHelper
{
	public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	public static string Serialize<T>(T value)
	{
		return JsonSerializer.Serialize(value, Options);
	}

	public static T? Deserialize<T>(string json)
	{
		return JsonSerializer.Deserialize<T>(json, Options);
	}
}
