using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyWatchlist.Api.Tests;

internal static class JsonTestOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
