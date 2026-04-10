using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
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
        var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
            DefaultExt = ".txt",
            FileName = "DiCOMpare_Report",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("DiCOMpare Report");
                sb.AppendLine(new string('=', 80));
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Source: {_vm.LeftPath}");
                sb.AppendLine($"Reference: {_vm.RightPath}");
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
                        sb.AppendLine($"\"{row.Safety}\",\"{row.Tag}\",\"{row.TagName}\",\"{Escape(row.LeftValue)}\",\"{Escape(row.RightValue)}\",\"{row.Status}\",\"{Escape(row.SafetyReason)}\"");
                    }
                }
                else
                {
                    sb.AppendLine($"{"Safety",-10} {"Tag",-16} {"Name",-30} {"Status",-12} Reason");
                    sb.AppendLine(new string('-', 80));
                    foreach (var row in _vm.FilteredRows)
                    {
                        if (!row.IsMismatch) continue;
                        sb.AppendLine($"{row.Safety,-10} {row.Tag,-16} {row.TagName,-30} {row.Status,-12} {row.SafetyReason}");
                        sb.AppendLine($"{"",10} Source:    {row.LeftValue}");
                        sb.AppendLine($"{"",10} Reference: {row.RightValue}");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(dialog.FileName, sb.ToString());
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
