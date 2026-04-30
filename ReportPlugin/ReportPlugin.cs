using System.Data;
using System.Drawing;
using FastReport;
using FastReport.Export.Image;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
// using Newtonsoft.Json; // 添加这一行

namespace ReportPlugin;

public class ReportPlugin
{
    private static readonly string[] SupportedFormats = ["pdf", "jpg", "png", "html"];

    public static string TemplateDirectory { get; set; } = GetDefaultTemplateDirectory();

    private static string GetDefaultTemplateDirectory()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var baseDir = string.IsNullOrEmpty(assemblyLocation)
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(baseDir, "templates");
    }

    private static void RegisterNorthWindData(Report report)
    {
        var nwindPath = Path.Combine(TemplateDirectory, "nwind.xml");
        if (!File.Exists(nwindPath))
        {
            throw new FileNotFoundException($"nwind.xml not found at: {nwindPath}");
        }

        var dataSet = new DataSet();
        dataSet.ReadXml(nwindPath);
        report.RegisterData(dataSet, "NorthWind");
    }

    public string GenerateReport(string reportName, string format = "pdf", string? outputPath = null)
    {
        format = format.ToLowerInvariant();
        if (!SupportedFormats.Contains(format))
        {
            throw new ArgumentException($"Unsupported format: {format}");
        }

        outputPath ??= Path.Combine(Directory.GetCurrentDirectory(), "export");
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{reportName}_{timestamp}.{format}";
        var filePath = Path.Combine(outputPath, fileName);
        var reportFile = Path.Combine(TemplateDirectory, reportName);

        if (!File.Exists(reportFile))
        {
            throw new FileNotFoundException($"Report template not found at: {reportFile}");
        }

        Report report = CreateReport(reportFile);

        try
        {
            report.Prepare();

            switch (format)
            {
                case "pdf":
                    report.Export(new PDFSimpleExport(), filePath);
                    break;
                case "jpg":
                case "png":
                    report.Export(new ImageExport { ImageFormat = format == "jpg" ? ImageExportFormat.Jpeg : ImageExportFormat.Png }, filePath);
                    break;
            }

            return filePath;
        }
        finally
        {
            report.Dispose();
        }
    }

    public string PrintReport(string reportName, string? printerName = null)
    {
        return GenerateReport(reportName, "pdf");
    }

    public string PreviewReport(string reportName, string format = "png")
    {
        format = format.ToLowerInvariant();
        if (format != "jpg" && format != "png")
        {
            format = "png";
        }

        Report report = CreateReport(reportName);

        try
        {
            report.Prepare();

            using var ms = new MemoryStream();
            report.Export(new ImageExport { ImageFormat = format == "jpg" ? ImageExportFormat.Jpeg : ImageExportFormat.Png }, ms);

            return $"data:image/{format};base64,{Convert.ToBase64String(ms.ToArray())}";
        }
        finally
        {
            report.Dispose();
        }
    }

    public string ConfigureReport(string reportName, string? configJson = null)
    {
        var config = new
        {
            reportName,
            availableFormats = SupportedFormats,
            supportedReportNames = GetReportNames(),
            customConfig = configJson ?? "{}"
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(config);
    }

    private Report CreateReport(string reportName)
    {
        var frxPath = Path.Combine(TemplateDirectory, reportName);
        if (File.Exists(frxPath))
        {
            var report = new Report();
            report.Load(frxPath);

            if (!reportName.Contains("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                RegisterNorthWindData(report);
            }

            return report;
        }

        return reportName switch
        {
            "SimpleList" => GetSimpleListReport(),
            "Group" => GetGroupReport(),
            _ => GetSimpleListReport()
        };
    }

    private Report GetSimpleListReport()
    {
        var report = new Report();
        var dataSet = CreateDemoDataSet();
        report.RegisterData(dataSet);
        report.GetDataSource("Employees").Enabled = true;

        var page = new ReportPage();
        report.Pages.Add(page);
        page.CreateUniqueName();

        page.ReportTitle = new ReportTitleBand();
        page.ReportTitle.Height = Units.Centimeters * 1;
        page.ReportTitle.CreateUniqueName();

        var titleText = new TextObject
        {
            Parent = page.ReportTitle,
            Bounds = new RectangleF(Units.Centimeters * 5, 0, Units.Centimeters * 10, Units.Centimeters * 1),
            Font = new Font("Arial", 14, FontStyle.Bold),
            Text = "Employee List",
            HorzAlign = HorzAlign.Center
        };
        titleText.CreateUniqueName();

        var dataBand = new DataBand
        {
            DataSource = report.GetDataSource("Employees"),
            Height = Units.Centimeters * 0.5f
        };
        page.Bands.Add(dataBand);
        dataBand.CreateUniqueName();

        var nameText = new TextObject
        {
            Parent = dataBand,
            Bounds = new RectangleF(0, 0, Units.Centimeters * 5, Units.Centimeters * 0.5f),
            Text = "[Employees.Name]",
            Font = new Font("Arial", 10)
        };
        nameText.CreateUniqueName();

        var deptText = new TextObject
        {
            Parent = dataBand,
            Bounds = new RectangleF(Units.Centimeters * 5.5f, 0, Units.Centimeters * 4, Units.Centimeters * 0.5f),
            Text = "[Employees.Department]",
            Font = new Font("Arial", 10)
        };
        deptText.CreateUniqueName();

        return report;
    }

    private Report GetGroupReport()
    {
        var report = new Report();
        var dataSet = CreateDemoDataSet();
        report.RegisterData(dataSet);
        report.GetDataSource("Employees").Enabled = true;

        var page = new ReportPage();
        report.Pages.Add(page);
        page.CreateUniqueName();

        var groupHeader = new GroupHeaderBand
        {
            Condition = "[Employees.Department]",
            SortOrder = FastReport.SortOrder.Ascending,
            Height = Units.Centimeters * 0.8f
        };
        page.Bands.Add(groupHeader);
        groupHeader.CreateUniqueName();

        var groupText = new TextObject
        {
            Parent = groupHeader,
            Bounds = new RectangleF(0, 0, Units.Centimeters * 10, Units.Centimeters * 0.8f),
            Font = new Font("Arial", 12, FontStyle.Bold),
            Text = "Department: [Employees.Department]",
            VertAlign = VertAlign.Center,
            Fill = new LinearGradientFill(Color.LightYellow, Color.White, 90)
        };
        groupText.CreateUniqueName();

        var dataBand = new DataBand
        {
            DataSource = report.GetDataSource("Employees"),
            Height = Units.Centimeters * 0.5f
        };
        groupHeader.Data = dataBand;
        dataBand.CreateUniqueName();

        var nameText = new TextObject
        {
            Parent = dataBand,
            Bounds = new RectangleF(0, 0, Units.Centimeters * 10, Units.Centimeters * 0.5f),
            Text = "[Employees.Name]",
            Font = new Font("Arial", 10)
        };
        nameText.CreateUniqueName();

        return report;
    }

    private static DataSet CreateDemoDataSet()
    {
        var ds = new DataSet();
        var dt = new DataTable("Employees");
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Department", typeof(string));

        dt.Rows.Add(1, "Alice", "Engineering");
        dt.Rows.Add(2, "Bob", "Engineering");
        dt.Rows.Add(3, "Charlie", "Marketing");
        dt.Rows.Add(4, "Diana", "Marketing");
        dt.Rows.Add(5, "Eve", "HR");
        dt.Rows.Add(6, "Frank", "HR");
        dt.Rows.Add(7, "Grace", "Engineering");
        dt.Rows.Add(8, "Hank", "Finance");

        ds.Tables.Add(dt);
        return ds;
    }

    public string GetInfo() => "ReportPlugin v1.0 - FastReport";

    public string[] GetReportNames() => new[] { "SimpleList", "Group", "Simple List.frx", "Master-Detail.frx", "Barcode.frx", "Groups.frx", "Simple Matrix.frx" };
}
