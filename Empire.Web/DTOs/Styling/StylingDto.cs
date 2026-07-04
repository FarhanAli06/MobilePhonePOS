namespace Empire.Web.DTOs.Styling;

public class StylingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "circle";
    public string Color { get; set; } = "#6c757d";
    public string TextColor { get; set; } = "#ffffff";
    public string BadgeVariant { get; set; } = "secondary";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class StylingSelectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "circle";
    public string Color { get; set; } = "#6c757d";
    public string TextColor { get; set; } = "#ffffff";
    public string BadgeVariant { get; set; } = "secondary";
}

public class CreateStylingRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public string? BadgeVariant { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateStylingRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public string? BadgeVariant { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

public class AssignStylingRequestDto
{
    public int? StylingId { get; set; }
}
