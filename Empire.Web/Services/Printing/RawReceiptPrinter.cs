using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Empire.Web.Services.Printing;

/// <summary>Sends ESC/POS receipt data to the dedicated Windows thermal-printer queue.</summary>
public static class RawReceiptPrinter
{
    public const string ReceiptPrinterName = "POS-80C";
    public const string GoogleReviewUrl = "https://share.google/F4ImjNMFpFy0YQTaq";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string DocumentName = "Empire Solution Receipt";
        [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string DataType = "RAW";
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocInfo docInfo);
    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr printer, byte[] bytes, int count, out int written);

    public static void Print(string receiptText, string? barcode = null, int copies = 1, string? logoPath = null)
    {
        if (!OpenPrinter(ReceiptPrinterName, out var printer, IntPtr.Zero))
            throw new InvalidOperationException($"Windows printer '{ReceiptPrinterName}' is unavailable (error {Marshal.GetLastWin32Error()}).");

        try
        {
            for (var copy = 0; copy < Math.Max(1, copies); copy++)
            {
                var bytes = BuildEscPos(receiptText, barcode, logoPath);
                var doc = new DocInfo();
                if (StartDocPrinter(printer, 1, doc) == 0)
                    throw new InvalidOperationException($"Could not start receipt job (error {Marshal.GetLastWin32Error()}).");
                try
                {
                    if (!StartPagePrinter(printer) || !WritePrinter(printer, bytes, bytes.Length, out var written) || written != bytes.Length)
                        throw new InvalidOperationException($"Could not send receipt data (error {Marshal.GetLastWin32Error()}).");
                    EndPagePrinter(printer);
                }
                finally { EndDocPrinter(printer); }
            }
        }
        finally { ClosePrinter(printer); }
    }

    /// <summary>Prints the compact intake slip used when a repair is first received.</summary>
    public static void PrintRepairDropOff(string slipText, string repairNumber, string? logoPath = null)
    {
        if (!OpenPrinter(ReceiptPrinterName, out var printer, IntPtr.Zero))
            throw new InvalidOperationException($"Windows printer '{ReceiptPrinterName}' is unavailable (error {Marshal.GetLastWin32Error()}).");

        try
        {
            var bytes = BuildDropOffEscPos(slipText, repairNumber, logoPath);
            var doc = new DocInfo { DocumentName = "Empire Repair Drop-Off" };
            if (StartDocPrinter(printer, 1, doc) == 0)
                throw new InvalidOperationException($"Could not start drop-off slip job (error {Marshal.GetLastWin32Error()}).");
            try
            {
                if (!StartPagePrinter(printer) || !WritePrinter(printer, bytes, bytes.Length, out var written) || written != bytes.Length)
                    throw new InvalidOperationException($"Could not send drop-off slip data (error {Marshal.GetLastWin32Error()}).");
                EndPagePrinter(printer);
            }
            finally { EndDocPrinter(printer); }
        }
        finally { ClosePrinter(printer); }
    }

    private static byte[] BuildDropOffEscPos(string text, string repairNumber, string? logoPath)
    {
        using var stream = new MemoryStream();
        void Add(params byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
        void Write(string value) { var bytes = Encoding.ASCII.GetBytes(value); stream.Write(bytes, 0, bytes.Length); }

        Add(0x1B, 0x40);                 // initialize
        Add(0x1B, 0x61, 0x01);           // centered
        WriteCompactLogo(logoPath);
        Add(0x1B, 0x45, 0x01);           // bold
        Add(0x1D, 0x21, 0x11);           // double size
        Write("REPAIR DROP-OFF\n");
        Add(0x1D, 0x21, 0x00);
        Add(0x1B, 0x45, 0x00);
        Write("Empire Phone Repair\n");
        Write("3920 Rosecrans Ave, Hawthorne, CA\n");
        Write("(310) 341-9456\n");
        Write("------------------------------------------\n");
        Add(0x1B, 0x61, 0x00);           // left
        Write(text.Replace("\r", string.Empty));
        if (!text.EndsWith('\n')) Write("\n");
        Write("------------------------------------------\n");

        var clean = new string((repairNumber ?? string.Empty).Where(c => c >= 32 && c <= 126).Take(60).ToArray());
        if (!string.IsNullOrWhiteSpace(clean))
        {
            Add(0x1B, 0x61, 0x01);
            Add(0x1D, 0x48, 0x02);       // repair number below barcode
            Add(0x1D, 0x68, 0x38);       // compact barcode height
            Add(0x1D, 0x77, 0x02);
            Add(0x1D, 0x6B, 0x49, (byte)clean.Length);
            Write(clean);
            Write("\n");
        }
        Write("\n");
        Add(0x1D, 0x56, 0x42, 0x00);     // partial cut
        return stream.ToArray();

        void WriteCompactLogo(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                using var source = new Bitmap(path);
                const int maxWidth = 190;
                const int maxHeight = 105;
                var scale = Math.Min(maxWidth / (double)source.Width, maxHeight / (double)source.Height);
                var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
                using var resized = new Bitmap(targetWidth, targetHeight);
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.Clear(Color.White);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(source, 0, 0, targetWidth, targetHeight);
                }
                var widthBytes = (targetWidth + 7) / 8;
                var raster = new byte[widthBytes * targetHeight];
                for (var y = 0; y < targetHeight; y++)
                for (var x = 0; x < targetWidth; x++)
                {
                    var pixel = resized.GetPixel(x, y);
                    var luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                    var isGoldCrown = pixel.R > 130 && pixel.G > 70 && pixel.B < 120 && pixel.R > pixel.B * 1.5;
                    if (pixel.A > 30 && (luminance < 155 || isGoldCrown)) raster[y * widthBytes + x / 8] |= (byte)(0x80 >> (x % 8));
                }
                Add(0x1D, 0x76, 0x30, 0x00, (byte)(widthBytes & 0xFF), (byte)(widthBytes >> 8), (byte)(targetHeight & 0xFF), (byte)(targetHeight >> 8));
                stream.Write(raster, 0, raster.Length);
                Write("\n");
            }
            catch { /* The slip still prints if the optional logo cannot be decoded. */ }
        }
    }

    private static byte[] BuildEscPos(string text, string? barcode, string? logoPath)
    {
        using var stream = new MemoryStream();
        void Add(params byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
        void Write(string value) { var bytes = Encoding.ASCII.GetBytes(value); stream.Write(bytes, 0, bytes.Length); }

        Add(0x1B, 0x40);                 // initialize
        Add(0x1B, 0x61, 0x01);           // centered
        WriteLogo(logoPath);
        Add(0x1B, 0x45, 0x01);           // bold
        Add(0x1D, 0x21, 0x11);           // double size
        Write("EMPIRE PHONE REPAIR\n");
        Add(0x1D, 0x21, 0x00);
        Add(0x1B, 0x45, 0x00);
        Write("3920 Rosecrans Ave\n");
        Write("Hawthorne, CA\n");
        Write("(310) 341-9456\n");
        Write("------------------------------------------\n");
        Add(0x1B, 0x61, 0x00);           // left
        Write(text.Replace("\r", string.Empty));
        if (!text.EndsWith('\n')) Write("\n");
        Write("------------------------------------------\n");
        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var clean = new string(barcode.Where(c => c >= 32 && c <= 126).Take(60).ToArray());
            Add(0x1B, 0x61, 0x01);
            Add(0x1D, 0x48, 0x02);       // text below barcode
            Add(0x1D, 0x68, 0x48);       // barcode height
            Add(0x1D, 0x77, 0x02);       // barcode width
            Add(0x1D, 0x6B, 0x49, (byte)clean.Length); // Code 128 auto
            Write(clean);
            Write("\n");
        }
        Add(0x1B, 0x61, 0x01);
        Add(0x1B, 0x45, 0x01);
        Write("LEAVE US A GOOGLE REVIEW\n");
        Add(0x1B, 0x45, 0x00);
        WriteQr(GoogleReviewUrl);
        Write("Scan to review Empire Phone Repair\n\n");
        Add(0x1B, 0x61, 0x01);
        WriteHandwrittenThankYou();
        Write("\n\n\n");
        Add(0x1D, 0x56, 0x42, 0x00);     // partial cut
        return stream.ToArray();

        void WriteLogo(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                using var source = new Bitmap(path);
                const int maxWidth = 300;
                const int maxHeight = 300;
                var scale = Math.Min(maxWidth / (double)source.Width, maxHeight / (double)source.Height);
                var targetWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                var targetHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
                using var resized = new Bitmap(targetWidth, targetHeight);
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.Clear(Color.White);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(source, 0, 0, targetWidth, targetHeight);
                }
                var widthBytes = (targetWidth + 7) / 8;
                var raster = new byte[widthBytes * targetHeight];
                for (var y = 0; y < targetHeight; y++)
                for (var x = 0; x < targetWidth; x++)
                {
                    var pixel = resized.GetPixel(x, y);
                    var luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                    var isGoldCrown = pixel.R > 130 && pixel.G > 70 && pixel.B < 120 && pixel.R > pixel.B * 1.5;
                    if (pixel.A > 30 && (luminance < 155 || isGoldCrown)) raster[y * widthBytes + x / 8] |= (byte)(0x80 >> (x % 8));
                }
                Add(0x1D, 0x76, 0x30, 0x00, (byte)(widthBytes & 0xFF), (byte)(widthBytes >> 8), (byte)(targetHeight & 0xFF), (byte)(targetHeight >> 8));
                stream.Write(raster, 0, raster.Length);
                Write("\n");
            }
            catch { /* A receipt must still print if the optional logo cannot be decoded. */ }
        }

        void WriteHandwrittenThankYou()
        {
            try
            {
                using var image = new Bitmap(360, 86);
                using (var graphics = Graphics.FromImage(image))
                using (var font = new Font("Segoe Script", 43, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(Color.Black))
                {
                    graphics.Clear(Color.White);
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    var text = "Thank You!";
                    var size = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, brush, Math.Max(0, (image.Width - size.Width) / 2), 4);
                }

                var widthBytes = (image.Width + 7) / 8;
                var raster = new byte[widthBytes * image.Height];
                for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    var luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                    if (luminance < 190) raster[y * widthBytes + x / 8] |= (byte)(0x80 >> (x % 8));
                }
                Add(0x1D, 0x76, 0x30, 0x00, (byte)(widthBytes & 0xFF), (byte)(widthBytes >> 8), (byte)(image.Height & 0xFF), (byte)(image.Height >> 8));
                stream.Write(raster, 0, raster.Length);
                Write("\n");
            }
            catch { Write("Thank You!\n"); }
        }

        void WriteQr(string value)
        {
            var data = Encoding.ASCII.GetBytes(value);
            Add(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00); // model 2
            Add(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x05);       // module size
            Add(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31);       // error correction M
            var length = data.Length + 3;
            Add(0x1D, 0x28, 0x6B, (byte)(length & 0xFF), (byte)(length >> 8), 0x31, 0x50, 0x30);
            stream.Write(data, 0, data.Length);
            Add(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30);       // print QR
            Write("\n");
        }
    }
}
