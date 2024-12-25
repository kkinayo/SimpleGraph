using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleGraph.Models
{
    public class TickerDepthMessage
    {
        private static JsonSerializerOptions Options;

        static TickerDepthMessage()
        {
            Options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        public static bool TryParse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            try
            {
                JsonSerializer.Deserialize<TickerDepthMessage>(str, Options);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static TickerDepthMessage Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException("Input string cannot be null or whitespace.", nameof(str));

            try
            {
                return JsonSerializer.Deserialize<TickerDepthMessage>(str, Options)
                       ?? throw new InvalidOperationException("Deserialized object is null.");
            }
            catch (JsonException ex)
            {
                throw new FormatException("Failed to parse the input string as TickerDepthMessage.", ex);
            }
        }

        [JsonPropertyName("u")]
        public long UupdateID { get; set; }

        [JsonPropertyName("s")]
        public string Symbol { get; set; }

        [JsonPropertyName("b")]
        public double BidPrice { get; set; }

        [JsonPropertyName("B")]
        public double BidQuantity { get; set; }

        [JsonPropertyName("a")]
        public double AskPrice { get; set; }

        [JsonPropertyName("A")]
        public double AskQuantity { get; set; }
    }
}