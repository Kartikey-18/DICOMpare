using DiCOMpare.Models;

namespace DiCOMpare.Services;

public static class ComparisonService
{
    public static List<ComparisonRow> Compare(List<DicomTagEntry> left, List<DicomTagEntry> right)
    {
        var leftDict = left.ToDictionary(t => t.Tag);
        var rightDict = right.ToDictionary(t => t.Tag);
        var allTags = leftDict.Keys.Union(rightDict.Keys).OrderBy(k => k).ToList();

        var results = new List<ComparisonRow>();

        foreach (var tag in allTags)
        {
            var hasLeft = leftDict.TryGetValue(tag, out var leftEntry);
            var hasRight = rightDict.TryGetValue(tag, out var rightEntry);

            var row = new ComparisonRow
            {
                Tag = tag,
                TagName = (leftEntry?.Name ?? rightEntry?.Name) ?? "Unknown",
                LeftValue = leftEntry?.Value ?? "",
                RightValue = rightEntry?.Value ?? "",
                Safety = leftEntry?.Safety ?? rightEntry?.Safety ?? TagSafety.Caution,
                SafetyReason = leftEntry?.SafetyReason ?? rightEntry?.SafetyReason ?? "",
            };

            if (!hasLeft)
                row.Status = MatchStatus.MissingLeft;
            else if (!hasRight)
                row.Status = MatchStatus.MissingRight;
            else if (leftEntry!.Value == rightEntry!.Value)
                row.Status = MatchStatus.Match;
            else
                row.Status = MatchStatus.Mismatch;

            results.Add(row);
        }

        return results;
    }

    public static ComparisonSummary Summarize(List<ComparisonRow> rows)
    {
        var mismatches = rows.Where(r => r.IsMismatch).ToList();

        return new ComparisonSummary
        {
            TotalTags = rows.Count,
            Matches = rows.Count(r => r.Status == MatchStatus.Match),
            Mismatches = mismatches.Count(r => r.Status == MatchStatus.Mismatch),
            MissingLeft = rows.Count(r => r.Status == MatchStatus.MissingLeft),
            MissingRight = rows.Count(r => r.Status == MatchStatus.MissingRight),
            UnsafeMismatches = mismatches.Count(r => r.Safety == TagSafety.Unsafe),
            CautionMismatches = mismatches.Count(r => r.Safety == TagSafety.Caution),
            SafeMismatches = mismatches.Count(r => r.Safety == TagSafety.Safe),
        };
    }
}

public class ComparisonSummary
{
    public int TotalTags { get; set; }
    public int Matches { get; set; }
    public int Mismatches { get; set; }
    public int MissingLeft { get; set; }
    public int MissingRight { get; set; }
    public int UnsafeMismatches { get; set; }
    public int CautionMismatches { get; set; }
    public int SafeMismatches { get; set; }

    public string Verdict
    {
        get
        {
            if (UnsafeMismatches > 0)
                return "INCOMPATIBLE - Unsafe tag differences detected. These studies are fundamentally different. Converting would affect measurements and diagnosis.";
            if (CautionMismatches > 0)
                return "REVIEW NEEDED - Some differences require careful review before any conversion.";
            if (SafeMismatches > 0)
                return "CONVERTIBLE - Only safe (routing/identification) tags differ. Conversion would not affect clinical data.";
            return "IDENTICAL - No meaningful differences found.";
        }
    }
}
