using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DiCOMpare.Models;
using DiCOMpare.Services;

namespace DiCOMpare.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private DicomStudyInfo? _leftStudy;
    private DicomStudyInfo? _rightStudy;
    private string _leftPath = string.Empty;
    private string _rightPath = string.Empty;
    private string _statusText = "Load two DICOM studies to compare.";
    private string _verdictText = string.Empty;
    private string _summaryText = string.Empty;
    private string _filterText = string.Empty;
    private bool _showOnlyDifferences;
    private bool _isCompared;

    public ObservableCollection<DicomTagEntry> LeftTags { get; } = new();
    public ObservableCollection<DicomTagEntry> RightTags { get; } = new();
    public ObservableCollection<ComparisonRow> ComparisonRows { get; } = new();
    public ObservableCollection<ComparisonRow> FilteredRows { get; } = new();

    public string LeftPath
    {
        get => _leftPath;
        set { _leftPath = value; OnPropertyChanged(); }
    }

    public string RightPath
    {
        get => _rightPath;
        set { _rightPath = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string VerdictText
    {
        get => _verdictText;
        set { _verdictText = value; OnPropertyChanged(); }
    }

    public string SummaryText
    {
        get => _summaryText;
        set { _summaryText = value; OnPropertyChanged(); }
    }

    public string FilterText
    {
        get => _filterText;
        set { _filterText = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool ShowOnlyDifferences
    {
        get => _showOnlyDifferences;
        set { _showOnlyDifferences = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool IsCompared
    {
        get => _isCompared;
        set { _isCompared = value; OnPropertyChanged(); }
    }

    public string LeftStudySummary => _leftStudy == null ? "" :
        $"{_leftStudy.Modality} | {_leftStudy.SOPClassName}\n" +
        $"Patient: {_leftStudy.PatientName} ({_leftStudy.PatientId})\n" +
        $"Study: {_leftStudy.StudyDescription}\n" +
        $"Series: {_leftStudy.SeriesDescription}\n" +
        $"Files: {_leftStudy.FileCount}";

    public string RightStudySummary => _rightStudy == null ? "" :
        $"{_rightStudy.Modality} | {_rightStudy.SOPClassName}\n" +
        $"Patient: {_rightStudy.PatientName} ({_rightStudy.PatientId})\n" +
        $"Study: {_rightStudy.StudyDescription}\n" +
        $"Series: {_rightStudy.SeriesDescription}\n" +
        $"Files: {_rightStudy.FileCount}";

    public async Task LoadLeftAsync(string path)
    {
        try
        {
            StatusText = "Loading left study...";
            LeftPath = path;
            _leftStudy = await DicomReaderService.ReadStudyAsync(path);
            LeftTags.Clear();
            foreach (var tag in _leftStudy.Tags)
                LeftTags.Add(tag);
            OnPropertyChanged(nameof(LeftStudySummary));
            StatusText = $"Left: Loaded {_leftStudy.FileCount} file(s), {_leftStudy.Tags.Count} tags.";
            TryCompare();
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading left study: {ex.Message}";
        }
    }

    public async Task LoadRightAsync(string path)
    {
        try
        {
            StatusText = "Loading right study...";
            RightPath = path;
            _rightStudy = await DicomReaderService.ReadStudyAsync(path);
            RightTags.Clear();
            foreach (var tag in _rightStudy.Tags)
                RightTags.Add(tag);
            OnPropertyChanged(nameof(RightStudySummary));
            StatusText = $"Right: Loaded {_rightStudy.FileCount} file(s), {_rightStudy.Tags.Count} tags.";
            TryCompare();
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading right study: {ex.Message}";
        }
    }

    private void TryCompare()
    {
        if (_leftStudy == null || _rightStudy == null)
            return;

        var rows = ComparisonService.Compare(_leftStudy.Tags, _rightStudy.Tags);
        var summary = ComparisonService.Summarize(rows);

        ComparisonRows.Clear();
        foreach (var row in rows)
            ComparisonRows.Add(row);

        VerdictText = summary.Verdict;
        SummaryText = $"Total: {summary.TotalTags} tags | " +
                      $"Matching: {summary.Matches} | " +
                      $"Different: {summary.Mismatches} | " +
                      $"Missing: {summary.MissingLeft + summary.MissingRight}\n" +
                      $"Unsafe differences: {summary.UnsafeMismatches} | " +
                      $"Caution: {summary.CautionMismatches} | " +
                      $"Safe: {summary.SafeMismatches}";

        IsCompared = true;
        StatusText = "Comparison complete.";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredRows.Clear();

        foreach (var row in ComparisonRows)
        {
            if (_showOnlyDifferences && !row.IsMismatch)
                continue;

            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                var search = _filterText.ToLowerInvariant();
                if (!row.Tag.ToLowerInvariant().Contains(search) &&
                    !row.TagName.ToLowerInvariant().Contains(search) &&
                    !row.LeftValue.ToLowerInvariant().Contains(search) &&
                    !row.RightValue.ToLowerInvariant().Contains(search))
                    continue;
            }

            FilteredRows.Add(row);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
