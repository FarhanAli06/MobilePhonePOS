namespace Empire.Web.DTOs.Pos
{
    public class POSRepairDto
    {
        public int Id { get; set; }
        public string RepairNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
