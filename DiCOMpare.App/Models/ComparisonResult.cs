namespace DiCOMpare.Models;

public enum MatchStatus
{
    Match,
    Mismatch,
    MissingLeft,
    MissingRight
}

public class ComparisonRow
{
    public string Tag { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string LeftValue { get; set; } = string.Empty;
    public string RightValue { get; set; } = string.Empty;
    public MatchStatus Status { get; set; }
    public TagSafety Safety { get; set; }
    public string SafetyReason { get; set; } = string.Empty;

    public bool IsMismatch => Status != MatchStatus.Match;
}
