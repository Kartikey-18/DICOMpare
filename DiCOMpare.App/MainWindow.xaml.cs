using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DiCOMpare.Services;
using DiCOMpare.ViewModels;
using Microsoft.Win32;

namespace DiCOMpare;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
    }

    private async void BrowseLeft_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder("Select Source DICOM Study Folder");
        if (path != null)
            await _vm.LoadLeftAsync(path);
    }

    private async void BrowseLeftFile_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFile("Select Source DICOM File");
        if (path != null)
            await _vm.LoadLeftAsync(path);
    }

    private async void BrowseRight_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder("Select Reference DICOM Study Folder");
        if (path != null)
            await _vm.LoadRightAsync(path);
    }

    private async void BrowseRightFile_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFile("Select Reference DICOM File");
        if (path != null)
            await _vm.LoadRightAsync(path);
    }

    private async void LeftDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                await _vm.LoadLeftAsync(files[0]);
        }
    }

    private async void RightDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                await _vm.LoadRightAsync(files[0]);
        }
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _vm.Theme.Toggle();
    }

    private void DragOverHandler(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        var redactPhi = RedactPhiCheckbox.IsChecked == true;

        var dialog = new SaveFileDialog
        {
            Filter = "PDF report (*.pdf)|*.pdf|Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
            DefaultExt = ".pdf",
            FileName = "DiCOMpare_Report",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (dialog.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    PdfExportService.Export(
                        dialog.FileName,
                        _vm.LeftPath,
                        _vm.RightPath,
                        _vm.LeftStudySummary,
                        _vm.RightStudySummary,
                        _vm.VerdictText,
                        _vm.SummaryText,
                        _vm.FilteredRows,
                        redactPhi);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("DiCOMpare Report");
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    if (redactPhi)
                    {
                        sb.AppendLine("PHI has been redacted from this report.");
                        sb.AppendLine($"Source: [Path redacted]");
                        sb.AppendLine($"Reference: [Path redacted]");
                    }
                    else
                    {
                        sb.AppendLine($"Source: {_vm.LeftPath}");
                        sb.AppendLine($"Reference: {_vm.RightPath}");
                    }
                    sb.AppendLine();
                    sb.AppendLine(_vm.VerdictText);
                    sb.AppendLine(_vm.SummaryText);
                    sb.AppendLine();
                    sb.AppendLine(new string('-', 80));

                    if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("Safety,Tag,Name,Source Value,Reference Value,Status,Reason");
                        foreach (var row in _vm.FilteredRows)
                        {
                            var leftVal = redactPhi ? PhiRedactionService.Redact(row.Tag, row.LeftValue) : row.LeftValue;
                            var rightVal = redactPhi ? PhiRedactionService.Redact(row.Tag, row.RightValue) : row.RightValue;
                            sb.AppendLine($"\"{row.Safety}\",\"{row.Tag}\",\"{row.TagName}\",\"{Escape(leftVal)}\",\"{Escape(rightVal)}\",\"{row.Status}\",\"{Escape(row.SafetyReason)}\"");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{"Safety",-10} {"Tag",-16} {"Name",-30} {"Status",-12} Reason");
                        sb.AppendLine(new string('-', 80));
                        foreach (var row in _vm.FilteredRows)
                        {
                            if (!row.IsMismatch) continue;
                            var leftVal = redactPhi ? PhiRedactionService.Redact(row.Tag, row.LeftValue) : row.LeftValue;
                            var rightVal = redactPhi ? PhiRedactionService.Redact(row.Tag, row.RightValue) : row.RightValue;
                            sb.AppendLine($"{row.Safety,-10} {row.Tag,-16} {row.TagName,-30} {row.Status,-12} {row.SafetyReason}");
                            sb.AppendLine($"{"",10} Source:    {leftVal}");
                            sb.AppendLine($"{"",10} Reference: {rightVal}");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine("For diagnostic purposes only. Not a clinical decision tool.");

                    File.WriteAllText(dialog.FileName, sb.ToString());
                }

                _vm.StatusText = $"Report exported to: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string Escape(string value) => value.Replace("\"", "\"\"");

    private static string? BrowseFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private static string? BrowseFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*",
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
