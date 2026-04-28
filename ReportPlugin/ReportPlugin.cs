using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using FastReport;
using FastReport.Export.Image;
using FastReport.Export.PdfSimple;
using FastReport.Utils;

namespace ReportPlugin;

public class ReportPlugin
{
    private static readonly string[] SupportedFormats = { "pdf", "jpg", "png" };

    /// <summary>
    /// 生成报表并导出到文件（打印/预览/导出）
    /// </summary>
    /// <param name="reportType">报表类型: SimpleList, Group</param>
    /// <param name="format">输出格式: pdf, jpg, png</param>
    /// <param name="outputPath">输出目录（可选，默认当前目录）</param>
    /// <returns>生成的文件路径</returns>
    public string GenerateReport(string reportType, string format = "pdf", string? outputPath = null)
    {
        format = format.ToLowerInvariant();
        if (!SupportedFormats.Contains(format))
        {
            throw new ArgumentException($"不支持的格式: {format}. 支持: {string.Join(", ", SupportedFormats)}");
        }

        outputPath ??= Directory.GetCurrentDirectory();
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{reportType}_{timestamp}.{format}";
        var filePath = Path.Combine(outputPath, fileName);

        Report report = CreateReport(reportType);

        try
        {
            report.Prepare();

            switch (format)
            {
                case "pdf":
                    var pdfExport = new PDFSimpleExport();
                    report.Export(pdfExport, filePath);
                    break;
                case "jpg":
                case "png":
                    var imageExport = new ImageExport();
                    imageExport.ImageFormat = format == "jpg" ? ImageExportFormat.Jpeg : ImageExportFormat.Png;
                    report.Export(imageExport, filePath);
                    break;
            }

            return filePath;
        }
        finally
        {
            report.Dispose();
        }
    }

    /// <summary>
    /// 打印报表 - 直接发送到默认打印机
    /// </summary>
    /// <param name="reportType">报表类型: SimpleList, Group</param>
    /// <param name="printerName">打印机名称（可选，默认使用默认打印机）</param>
    /// <summary>
    /// 打印报表 - 直接发送到默认打印机
    /// 注意: FastReport.OpenSource 不支持直接打印，请使用 GenerateReport 生成 PDF 后手动打印
    /// </summary>
    /// <param name="reportType">报表类型: SimpleList, Group</param>
    /// <param name="printerName">打印机名称（可选，默认使用默认打印机）</param>
    /// <returns>生成的 PDF 文件路径，可通过系统打印</returns>
    public string PrintReport(string reportType, string? printerName = null)
    {
        var filePath = GenerateReport(reportType, "pdf");
        return filePath;
    }

    /// <summary>
    /// 预览报表 - 返回 Base64 编码的图片用于前端显示
    /// </summary>
    /// <param name="reportType">报表类型: SimpleList, Group</param>
    /// <param name="format">图片格式: jpg, png</param>
    /// <returns>Base64 编码的图片字符串</returns>
    public string PreviewReport(string reportType, string format = "png")
    {
        format = format.ToLowerInvariant();
        if (format != "jpg" && format != "png")
        {
            format = "png";
        }

        Report report = CreateReport(reportType);

        try
        {
            report.Prepare();

            using var ms = new MemoryStream();
            var imageExport = new ImageExport
            {
                ImageFormat = format == "jpg" ? ImageExportFormat.Jpeg : ImageExportFormat.Png
            };
            report.Export(imageExport, ms);

            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return $"data:image/{format};base64,{base64}";
        }
        finally
        {
            report.Dispose();
        }
    }

    /// <summary>
    /// 设置报表 - 配置报表参数并返回配置信息
    /// </summary>
    /// <param name="reportType">报表类型</param>
    /// <param name="configJson">JSON 格式的配置参数</param>
    /// <returns>配置结果 JSON</returns>
    public string ConfigureReport(string reportType, string? configJson = null)
    {
        var config = new
        {
            reportType,
            availableFormats = SupportedFormats,
            supportedReportTypes = GetReportTypes(),
            customConfig = configJson ?? "{}"
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(config);
    }

    private Report CreateReport(string reportType)
    {
        return reportType switch
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
            Text = "员工列表 / Employee List",
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
            Text = "部门: [Employees.Department]",
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

    private DataSet CreateDemoDataSet()
    {
        var ds = new DataSet();
        var dt = new DataTable("Employees");
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Department", typeof(string));

        dt.Rows.Add(1, "张三", "研发部");
        dt.Rows.Add(2, "李四", "研发部");
        dt.Rows.Add(3, "王五", "市场部");
        dt.Rows.Add(4, "赵六", "市场部");
        dt.Rows.Add(5, "钱七", "人事部");
        dt.Rows.Add(6, "孙八", "人事部");
        dt.Rows.Add(7, "周九", "研发部");
        dt.Rows.Add(8, "吴十", "财务部");

        ds.Tables.Add(dt);
        return ds;
    }

    public string GetInfo() => "ReportPlugin v1.0 - FastReport";

    public string[] GetReportTypes() => new[] { "SimpleList", "Group" };
}