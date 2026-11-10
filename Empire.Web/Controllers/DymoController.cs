using DymoSDK.Implementations;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Web.Controllers;

[ApiController]
[Route("Dymo")]
public sealed class DymoController : ControllerBase
{
    private static readonly SemaphoreSlim PrintLock = new(1, 1);
    private readonly IWebHostEnvironment _environment;

    public DymoController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("Status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            DymoSDK.App.Init();
            DymoSDK.Interfaces.IPrinter? printer = null;
            for (var attempt = 0; attempt < 5 && printer is null; attempt++)
            {
                printer = DymoPrinter.Instance.GetPrinters().FirstOrDefault(candidate => candidate.IsConnected);
                if (printer is null) await Task.Delay(500);
            }
            return printer is null
                ? StatusCode(503, new { connected = false })
                : Ok(new { connected = true, printer = printer.Name });
        }
        catch (Exception exception)
        {
            return StatusCode(503, new { connected = false, message = exception.Message });
        }
    }

    [HttpPost("PrintLabel")]
    public async Task<IActionResult> PrintLabel([FromBody] DymoLabelRequest request)
    {
        await PrintLock.WaitAsync();
        try
        {
            DymoSDK.App.Init();

            DymoSDK.Interfaces.IPrinter? printer = null;
            for (var attempt = 0; attempt < 5 && printer is null; attempt++)
            {
                printer = DymoPrinter.Instance.GetPrinters()
                    .FirstOrDefault(candidate => candidate.IsConnected);
                if (printer is null) await Task.Delay(500);
            }

            if (printer is null)
                return StatusCode(503, new { message = "No connected DYMO printer was found." });

            var isDeviceLabel = string.Equals(request.LabelType, "device", StringComparison.OrdinalIgnoreCase);
            var labelPath = Path.Combine(
                _environment.ContentRootPath,
                "DymoLabels",
                isDeviceLabel ? "EmpireDevice30334.dymo" : "Empire30334.dymo");
            var label = DymoLabel.LabelSharedInstance;
            var sellingPrice = request.SellingPrice.HasValue
                ? request.SellingPrice.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
                : "$0.00";
            if (isDeviceLabel)
            {
                var modelName = (request.Model ?? request.Primary ?? "Device").Trim();
                label.LoadLabelFromXML(ConfigureDeviceLabelForModel(
                    System.IO.File.ReadAllText(labelPath), modelName, sellingPrice));
            }
            else
            {
                label.LoadLabelFromFilePath(labelPath);
            }
            var customerNameObject = label.GetLabelObject("CustomerName");
            var detailsObject = label.GetLabelObject("RepairDetails");
            var barcodeObject = label.GetLabelObject("RepairBarcode");
            var sellingPriceObject = isDeviceLabel ? label.GetLabelObject("SellingPrice") : null;
            if (customerNameObject is null || detailsObject is null || barcodeObject is null ||
                (isDeviceLabel && sellingPriceObject is null))
                return StatusCode(500, new { message = "The DYMO label template is invalid." });

            var customerName = isDeviceLabel
                ? (request.Model ?? request.Primary ?? "Device").Trim()
                : FormatLabelCustomerName(request.CustomerName ?? request.Secondary ?? "Customer");
            var barcode = request.Barcode ?? request.Code ?? "N/A";
            string content;
            if (isDeviceLabel)
            {
                var storageValue = string.IsNullOrWhiteSpace(request.Storage) ? "N/A" : request.Storage.Trim();
                var storage = storageValue.Contains("GB", StringComparison.OrdinalIgnoreCase)
                    ? storageValue
                    : $"{storageValue} GB";
                var batteryValue = string.IsNullOrWhiteSpace(request.BatteryHealth)
                    ? "N/A"
                    : request.BatteryHealth.Trim().TrimEnd('%');
                var battery = $"Battery {batteryValue}{(batteryValue == "N/A" ? string.Empty : "%")}";
                var carrier = string.IsNullOrWhiteSpace(request.Carrier) ? "Carrier N/A" : request.Carrier.Trim();
                var lockStatus = string.IsNullOrWhiteSpace(request.NetworkStatus) ? "Lock N/A" : request.NetworkStatus.Trim();
                var details = $"{storage}  |  {battery}{Environment.NewLine}{carrier}  |  {lockStatus}";
                var notes = (request.Notes ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
                content = string.IsNullOrWhiteSpace(notes)
                    ? details
                    : $"{details}{Environment.NewLine}Notes: {notes}";
            }
            else
            {
                var phone = request.Phone ?? "N/A";
                var model = request.Model ?? request.Primary ?? "Device";
                var date = request.Date ?? DateTime.Now.ToString("MM/dd/yyyy");
                var repairType = string.IsNullOrWhiteSpace(request.RepairType) ? "Repair" : request.RepairType.Trim();
                content = string.Join(Environment.NewLine, new[] { phone, model, repairType, date });
            }

            if (!label.SetImageFromFilePath("EmpireLogo", Path.Combine(_environment.ContentRootPath, "DymoLabels", "EmpireLogo.png")) ||
                !label.UpdateLabelObject(customerNameObject, customerName) ||
                !label.UpdateLabelObject(detailsObject, content) ||
                !label.UpdateLabelObject(barcodeObject, barcode))
                return StatusCode(500, new { message = "The DYMO label could not be populated." });

            if (isDeviceLabel)
            {
                if (!label.UpdateLabelObject(sellingPriceObject!, sellingPrice))
                    return StatusCode(500, new { message = "The DYMO device price could not be populated." });
            }

            var accepted = await DymoPrinter.Instance.PrintLabel(
                label, printer.Name, 1, false, false, 0, false, false);

            if (!accepted)
                return StatusCode(502, new { message = "The DYMO printer rejected the label." });

            return Ok(new { printer = printer.Name, accepted = true });
        }
        catch (Exception exception)
        {
            return StatusCode(500, new { message = exception.Message });
        }
        finally
        {
            PrintLock.Release();
        }
    }

    [HttpPost("PreviewDeviceLabel")]
    public IActionResult PreviewDeviceLabel([FromBody] DymoLabelRequest request)
    {
        var modelName = (request.Model ?? request.Primary ?? "Device").Trim();
        var sellingPrice = request.SellingPrice.HasValue
            ? request.SellingPrice.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
            : "$0.00";
        var fontSize = CalculateDeviceHeaderFontSize(modelName, sellingPrice);

        return Ok(new
        {
            model = modelName,
            sellingPrice,
            fontSize,
            labelWidthInches = 2.25m,
            labelHeightInches = 1.25m,
            fontFamily = "Arial",
            fontWeight = "bold"
        });
    }

    private static string FormatLabelCustomerName(string value)
    {
        var names = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length < 2) return names.FirstOrDefault() ?? "Customer";
        return $"{names[0]} {char.ToUpperInvariant(names[^1][0])}.";
    }

    private static string ConfigureDeviceLabelForModel(string labelXml, string modelName, string sellingPrice)
    {
        var fontSize = CalculateDeviceHeaderFontSize(modelName, sellingPrice);

        var document = System.Xml.Linq.XDocument.Parse(labelXml);
        var headerObjects = document
            .Descendants("TextObject")
            .Where(textObject =>
            {
                var name = textObject.Element("Name")?.Value;
                return name is "CustomerName" or "SellingPrice";
            });

        foreach (var headerObject in headerObjects)
        {
            foreach (var fontSizeElement in headerObject.Descendants("FontSize"))
                fontSizeElement.Value = fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return document.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
    }

    private static int CalculateDeviceHeaderFontSize(string modelName, string sellingPrice)
    {
        const float dpi = 300f;
        const float modelWidthPixels = 2.03f * dpi;
        const float priceWidthPixels = 0.86f * dpi;
        const float heightPixels = 0.21f * dpi;

        try
        {
            using var bitmap = new System.Drawing.Bitmap(1, 1);
            bitmap.SetResolution(dpi, dpi);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var format = (System.Drawing.StringFormat)System.Drawing.StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= System.Drawing.StringFormatFlags.NoWrap;

            for (var fontSize = 16; fontSize >= 7; fontSize--)
            {
                using var font = new System.Drawing.Font(
                    "Arial", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
                var modelSize = graphics.MeasureString(modelName, font, int.MaxValue, format);
                var priceSize = graphics.MeasureString(sellingPrice, font, int.MaxValue, format);
                if (modelSize.Width <= modelWidthPixels && priceSize.Width <= priceWidthPixels &&
                    Math.Max(modelSize.Height, priceSize.Height) <= heightPixels)
                    return fontSize;
            }
        }
        catch
        {
        }

        return 7;
    }

    // Staff cost cipher: 0-9 map to BLACKHORSE. Example $125.00 => LAH.BB.
    private static string EncodeStaffCost(decimal? cost)
    {
        if (!cost.HasValue) return "N/A";
        const string key = "BLACKHORSE";
        var amount = decimal.Max(0, decimal.Round(cost.Value, 2, MidpointRounding.AwayFromZero));
        return string.Concat(amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            .Select(character => char.IsDigit(character) ? key[character - '0'] : character));
    }

    public sealed class DymoLabelRequest
    {
        public string? Primary { get; init; }
        public string? CodeLabel { get; init; }
        public string? Code { get; init; }
        public string? Secondary { get; init; }
        public string? Amount { get; init; }
        public string? Note { get; init; }
        public string? CustomerName { get; init; }
        public string? Phone { get; init; }
        public string? Model { get; init; }
        public string? Date { get; init; }
        public string? Barcode { get; init; }
        public string? RepairType { get; init; }
        public string? LabelType { get; init; }
        public string? Storage { get; init; }
        public string? Carrier { get; init; }
        public string? NetworkStatus { get; init; }
        public decimal? BuyingPrice { get; init; }
        public decimal? SellingPrice { get; init; }
        public string? BatteryHealth { get; init; }
        public string? Notes { get; init; }
    }
}
