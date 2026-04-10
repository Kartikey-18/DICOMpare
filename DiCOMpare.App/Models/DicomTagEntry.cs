namespace DiCOMpare.Models;

public class DicomTagEntry
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VR { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public TagSafety Safety { get; set; }
    public string SafetyReason { get; set; } = string.Empty;
}
