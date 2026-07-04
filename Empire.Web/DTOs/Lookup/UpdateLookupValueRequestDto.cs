namespace Empire.Web.DTOs.Lookup
{
    public class UpdateLookupValueRequestDto
    {
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ColorCode { get; set; }
        public int? CategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
