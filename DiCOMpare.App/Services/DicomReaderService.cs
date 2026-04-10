using System.IO;
using FellowOakDicom;
using DiCOMpare.Models;

namespace DiCOMpare.Services;

public class DicomStudyInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string SOPClassName { get; set; } = string.Empty;
    public string StudyDescription { get; set; } = string.Empty;
    public string SeriesDescription { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int InconsistentTagCount { get; set; }
    public List<DicomTagEntry> Tags { get; set; } = new();
}

public static class DicomReaderService
{
    // Tags that are expected to vary per-instance (not real inconsistencies)
    private static readonly HashSet<string> PerInstanceTags = new()
    {
        DicomTag.SOPInstanceUID.ToString(),
        DicomTag.InstanceNumber.ToString(),
        DicomTag.InstanceCreationDate.ToString(),
        DicomTag.InstanceCreationTime.ToString(),
        DicomTag.ContentDate.ToString(),
        DicomTag.ContentTime.ToString(),
        DicomTag.AcquisitionDate.ToString(),
        DicomTag.AcquisitionTime.ToString(),
        DicomTag.AcquisitionNumber.ToString(),
        DicomTag.SliceLocation.ToString(),
        DicomTag.ImagePositionPatient.ToString(),
        DicomTag.MediaStorageSOPInstanceUID.ToString(),
    };

    public static async Task<DicomStudyInfo> ReadStudyAsync(string path)
    {
        var files = GetDicomFiles(path);
        if (files.Count == 0)
            throw new InvalidOperationException($"No DICOM files found in: {path}");

        // Read the first file for representative tags
        var firstFile = await DicomFile.OpenAsync(files[0]);
        var dataset = firstFile.Dataset;

        var info = new DicomStudyInfo
        {
            FilePath = path,
            PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, ""),
            PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, ""),
            Modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, ""),
            SOPClassName = GetSOPClassName(dataset.GetSingleValueOrDefault(DicomTag.SOPClassUID, "")),
            StudyDescription = dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, ""),
            SeriesDescription = dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, ""),
            FileCount = files.Count,
        };

        // Extract tags from first file
        info.Tags = ExtractTags(dataset);

        // If multiple files, scan all and flag inconsistencies
        if (files.Count > 1)
        {
            await AnalyzeConsistency(info, files);
        }

        return info;
    }

    private static async Task AnalyzeConsistency(DicomStudyInfo info, List<string> files)
    {
        // Collect all values for each tag across all files
        // Key: tag string, Value: set of distinct values seen
        var tagValues = new Dictionary<string, HashSet<string>>();

        foreach (var tag in info.Tags)
        {
            tagValues[tag.Tag] = new HashSet<string> { tag.Value };
        }

        // Sample up to 20 files (or all if fewer) to keep it fast
        var sampled = files.Count <= 20 ? files : SampleFiles(files, 20);

        foreach (var filePath in sampled.Skip(1)) // skip first, already read
        {
            try
            {
                var file = await DicomFile.OpenAsync(filePath);
                var ds = file.Dataset;

                foreach (var item in ds)
                {
                    if (item.Tag == DicomTag.PixelData) continue;

                    var tagStr = item.Tag.ToString();
                    string value;

                    if (item.ValueRepresentation == DicomVR.SQ)
                    {
                        var seq = ds.GetSequence(item.Tag);
                        value = $"[Sequence: {seq.Items.Count} item(s)]";
                    }
                    else
                    {
                        try
                        {
                            value = string.Join("\\", ds.GetValues<string>(item.Tag));
                            if (value.Length > 200) value = value[..200] + "...";
                        }
                        catch
                        {
                            value = "[Unable to read value]";
                        }
                    }

                    if (tagValues.TryGetValue(tagStr, out var set))
                        set.Add(value);
                    else
                        tagValues[tagStr] = new HashSet<string> { value };
                }
            }
            catch
            {
                // Skip unreadable files
            }
        }

        // Mark inconsistent tags
        int inconsistentCount = 0;
        foreach (var tag in info.Tags)
        {
            if (tagValues.TryGetValue(tag.Tag, out var values) && values.Count > 1)
            {
                // Don't flag tags that are expected to vary per instance
                if (PerInstanceTags.Contains(tag.Tag))
                    continue;

                tag.IsInconsistent = true;
                tag.DistinctValueCount = values.Count;
                tag.Value += $" [{values.Count} distinct values across files]";
                inconsistentCount++;
            }
        }

        info.InconsistentTagCount = inconsistentCount;
    }

    private static List<string> SampleFiles(List<string> files, int count)
    {
        // Take first, last, and evenly spaced in between
        var result = new List<string> { files[0] };
        var step = (double)(files.Count - 1) / (count - 1);
        for (int i = 1; i < count - 1; i++)
        {
            result.Add(files[(int)(i * step)]);
        }
        result.Add(files[^1]);
        return result;
    }

    public static List<string> GetDicomFiles(string path)
    {
        if (File.Exists(path) && IsDicomFile(path))
            return new List<string> { path };

        if (!Directory.Exists(path))
            return new List<string>();

        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(IsDicomFile)
            .OrderBy(f => f)
            .ToList();
    }

    private static bool IsDicomFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".dcm" || ext == ".dicom")
            return true;

        // Check for extensionless DICOM files by reading the preamble
        if (string.IsNullOrEmpty(ext) || ext == ".")
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                if (stream.Length < 132) return false;
                stream.Seek(128, SeekOrigin.Begin);
                var magic = new byte[4];
                stream.Read(magic, 0, 4);
                return magic[0] == 'D' && magic[1] == 'I' && magic[2] == 'C' && magic[3] == 'M';
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static List<DicomTagEntry> ExtractTags(DicomDataset dataset)
    {
        var entries = new List<DicomTagEntry>();

        foreach (var item in dataset)
        {
            if (item.Tag == DicomTag.PixelData)
            {
                var (safety, reason) = TagSafetyClassifier.Classify(item.Tag);
                entries.Add(new DicomTagEntry
                {
                    Tag = item.Tag.ToString(),
                    Name = item.Tag.DictionaryEntry.Name,
                    VR = item.ValueRepresentation.Code,
                    Value = "[Pixel Data]",
                    Safety = safety,
                    SafetyReason = reason,
                });
                continue;
            }

            if (item.ValueRepresentation == DicomVR.SQ)
            {
                var seq = dataset.GetSequence(item.Tag);
                var (safety, reason) = TagSafetyClassifier.Classify(item.Tag);
                entries.Add(new DicomTagEntry
                {
                    Tag = item.Tag.ToString(),
                    Name = item.Tag.DictionaryEntry.Name,
                    VR = "SQ",
                    Value = $"[Sequence: {seq.Items.Count} item(s)]",
                    Safety = safety,
                    SafetyReason = reason,
                });
                continue;
            }

            try
            {
                var value = string.Join("\\", dataset.GetValues<string>(item.Tag));
                var (tagSafety, tagReason) = TagSafetyClassifier.Classify(item.Tag);

                entries.Add(new DicomTagEntry
                {
                    Tag = item.Tag.ToString(),
                    Name = item.Tag.DictionaryEntry.Name,
                    VR = item.ValueRepresentation.Code,
                    Value = value.Length > 200 ? value[..200] + "..." : value,
                    Safety = tagSafety,
                    SafetyReason = tagReason,
                });
            }
            catch
            {
                var (tagSafety, tagReason) = TagSafetyClassifier.Classify(item.Tag);
                entries.Add(new DicomTagEntry
                {
                    Tag = item.Tag.ToString(),
                    Name = item.Tag.DictionaryEntry.Name,
                    VR = item.ValueRepresentation.Code,
                    Value = "[Unable to read value]",
                    Safety = tagSafety,
                    SafetyReason = tagReason,
                });
            }
        }

        return entries.OrderBy(e => e.Tag).ToList();
    }

    private static string GetSOPClassName(string uid)
    {
        var knownClasses = new Dictionary<string, string>
        {
            ["1.2.840.10008.5.1.4.1.1.6.1"] = "Ultrasound Image Storage",
            ["1.2.840.10008.5.1.4.1.1.6.2"] = "Enhanced US Volume Storage",
            ["1.2.840.10008.5.1.4.1.1.3.1"] = "Ultrasound Multi-frame Image Storage",
            ["1.2.840.10008.5.1.4.1.1.7"] = "Secondary Capture Image Storage",
            ["1.2.840.10008.5.1.4.1.1.2"] = "CT Image Storage",
            ["1.2.840.10008.5.1.4.1.1.2.1"] = "Enhanced CT Image Storage",
            ["1.2.840.10008.5.1.4.1.1.1"] = "Computed Radiography Image Storage",
            ["1.2.840.10008.5.1.4.1.1.1.1"] = "Digital X-Ray Image Storage",
            ["1.2.840.10008.5.1.4.1.1.4"] = "MR Image Storage",
            ["1.2.840.10008.5.1.4.1.1.4.1"] = "Enhanced MR Image Storage",
            ["1.2.840.10008.5.1.4.1.1.12.1"] = "X-Ray Angiographic Image Storage",
            ["1.2.840.10008.5.1.4.1.1.77.1.1"] = "VL Endoscopic Image Storage",
            ["1.2.840.10008.5.1.4.1.1.88.11"] = "Basic Text SR Storage",
            ["1.2.840.10008.5.1.4.1.1.88.22"] = "Enhanced SR Storage",
            ["1.2.840.10008.5.1.4.1.1.104.1"] = "Encapsulated PDF Storage",
        };

        return knownClasses.TryGetValue(uid, out var name) ? name : uid;
    }
}
