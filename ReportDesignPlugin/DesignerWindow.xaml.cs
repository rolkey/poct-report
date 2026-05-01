using System.IO;
using System.Windows;
using FastReport;
using FastReport.Design.StandardDesigner;
using FastReport.Utils;
using Forms = System.Windows.Forms;

namespace ReportDesignPlugin;

public partial class DesignerWindow : Window
{
    private DesignerControl? _designerControl;
    private Report? _report;
    private string? _currentFilePath;

    public string? SavedFilePath { get; private set; }
    public string? SavedReportXml { get; private set; }

    public DesignerWindow()
    {
        InitializeComponent();
        Loaded += DesignerWindow_Loaded;
    }

    public void LoadReport(string filePath)
    {
        _currentFilePath = filePath;
        if (_report != null)
        {
            _report.Load(filePath);
            _designerControl!.Report = _report;
            UpdateTitle();
        }
    }

    public void LoadReport(Report report, string? filePath = null)
    {
        _currentFilePath = filePath;
        _report = report;
        if (_designerControl != null)
        {
            _designerControl.Report = report;
            UpdateTitle();
        }
    }

    private void DesignerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _designerControl = new DesignerControl
            {
                Dock = Forms.DockStyle.Fill,
                AskSave = true,
                ShowMainMenu = true,
                UIStyle = UIStyle.Office2007Blue
            };

            _report ??= new Report();
            _designerControl.Report = _report;
            _designerControl.RefreshLayout();
            _designerControl.UIStateChanged += DesignerControl_UIStateChanged;

            designerHost.Child = _designerControl;

            UpdateTitle();
            statusText.Text = "设计器已加载";
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show($"加载设计器失败: {ex.Message}\n\n请确保 FastReport 商业版 DLL 可用。",
                "错误", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            statusText.Text = "设计器加载失败";
        }
    }

    private void DesignerControl_UIStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_designerControl != null)
            {
                btnSave.IsEnabled = _designerControl.cmdSave.Enabled;
                btnUndo.IsEnabled = _designerControl.cmdUndo.Enabled;
                btnRedo.IsEnabled = _designerControl.cmdRedo.Enabled;
            }
        });
    }

    private void UpdateTitle()
    {
        var fileName = _currentFilePath != null
            ? Path.GetFileName(_currentFilePath)
            : "未命名报表";
        Title = $"FastReport 报表设计器 - {fileName}";
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        _designerControl?.cmdNew.Invoke();
        _currentFilePath = null;
        UpdateTitle();
    }

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        _designerControl?.cmdOpen.Invoke();
        if (_designerControl?.Report != null && !string.IsNullOrEmpty(_designerControl.Report.FileName))
        {
            _currentFilePath = _designerControl.Report.FileName;
            UpdateTitle();
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _designerControl?.cmdSave.Invoke();
        if (_designerControl?.Report != null && !string.IsNullOrEmpty(_designerControl.Report.FileName))
        {
            _currentFilePath = _designerControl.Report.FileName;
            UpdateTitle();
        }
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        _designerControl?.cmdUndo.Invoke();
    }

    private void BtnRedo_Click(object sender, RoutedEventArgs e)
    {
        _designerControl?.cmdRedo.Invoke();
    }

    private void BtnPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_report == null) return;

        try
        {
            statusText.Text = "正在准备预览...";
            _report.Preview = null;
            _report.Show();
            statusText.Text = "预览已关闭";
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show($"预览失败: {ex.Message}", "错误",
                Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            statusText.Text = "预览失败";
        }
    }

    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_report == null) return;

        try
        {
            var dialog = new Forms.SaveFileDialog
            {
                Filter = "PDF 文件 (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = !string.IsNullOrEmpty(_currentFilePath)
                    ? Path.GetFileNameWithoutExtension(_currentFilePath) + ".pdf"
                    : "report.pdf"
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                statusText.Text = "正在导出 PDF...";
                _report.Prepare();
                _report.Export(new FastReport.Export.Pdf.PDFExport(), dialog.FileName);
                statusText.Text = $"PDF 已导出: {dialog.FileName}";
            }
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show($"导出失败: {ex.Message}", "错误",
                Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            statusText.Text = "导出失败";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (_report != null)
        {
            if (!string.IsNullOrEmpty(_report.FileName))
            {
                SavedFilePath = _report.FileName;
            }

            try
            {
                using var ms = new MemoryStream();
                _report.Save(ms);
                ms.Position = 0;
                using var reader = new StreamReader(ms);
                SavedReportXml = reader.ReadToEnd();
            }
            catch
            {
            }
        }

        _designerControl?.Dispose();
        _designerControl = null;
    }
}
