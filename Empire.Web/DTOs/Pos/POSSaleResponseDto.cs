using System.Text.Json.Serialization;

namespace Empire.Web.DTOs.Pos
{
    public class POSSaleResponseDto
    {
        // API returns { data: { id, saleNumber, invoiceNumber, ... } }
        // "id" maps here (camelCase deserialization)
        public int Id { get; set; }

        // Legacy field kept for backwards compat (some code may set SaleId directly)
        public int SaleId { get; set; }

        public string SaleNumber { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;

        // The API top-level response has success=true but the data object does not;
        // we derive Success from whether Id > 0.
        [JsonIgnore]
        public bool Success => Id > 0 || SaleId > 0;

        public string? Message { get; set; }
    }
}
