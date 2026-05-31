using System.Text.Json.Serialization;

namespace BookBlossom.Core.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VoucherDiscountType
    {
        Fixed,
        Percentage
    }
}

