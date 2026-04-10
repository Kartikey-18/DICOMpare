using System.Collections.ObjectModel;
using DiCOMpare.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DiCOMpare.Services;

public static class PdfExportService
{
    public static void Export(
        string filePath,
        string leftPath,
        string rightPath,
        string leftSummary,
        string rightSummary,
        string verdict,
        string summary,
        ObservableCollection<ComparisonRow> rows,
        bool redactPhi)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("DiCOMpare Report").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                    if (redactPhi)
                        col.Item().Text("PHI has been redacted from this report.").FontSize(8).FontColor(Colors.Red.Medium).Bold();
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Study info side by side
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("SOURCE STUDY").Bold().FontSize(9).FontColor(Colors.Red.Darken1);
                            c.Item().Text(redactPhi ? "[Path redacted]" : leftPath).FontSize(7).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text(redactPhi ? RedactSummary(leftSummary) : leftSummary).FontSize(7);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("REFERENCE STUDY").Bold().FontSize(9).FontColor(Colors.Green.Darken1);
                            c.Item().Text(redactPhi ? "[Path redacted]" : rightPath).FontSize(7).FontColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(2).Text(redactPhi ? RedactSummary(rightSummary) : rightSummary).FontSize(7);
                        });
                    });

                    col.Item().PaddingVertical(6);

                    // Verdict
                    var verdictColor = verdict.StartsWith("INCOMPATIBLE") ? Colors.Red.Darken1
                        : verdict.StartsWith("REVIEW") ? Colors.Orange.Darken1
                        : Colors.Green.Darken1;
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                    {
                        c.Item().Text(verdict).Bold().FontSize(10).FontColor(verdictColor);
                        c.Item().Text(summary).FontSize(8).FontColor(Colors.Grey.Darken2);
                    });

                    col.Item().PaddingVertical(6);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(55);   // Safety
                        columns.ConstantColumn(90);   // Tag
                        columns.RelativeColumn(1.5f); // Name
                        columns.RelativeColumn(2);    // Source
                        columns.RelativeColumn(2);    // Reference
                        columns.ConstantColumn(60);   // Status
                    });

                    // Header
                    table.Header(header =>
                    {
                        var headerStyle = TextStyle.Default.Bold().FontSize(8).FontColor(Colors.White);

                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Safety").Style(headerStyle);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Tag").Style(headerStyle);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Name").Style(headerStyle);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Source").Style(headerStyle);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Reference").Style(headerStyle);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Status").Style(headerStyle);
                    });

                    // Rows
                    foreach (var r in rows)
                    {
                        var bgColor = r.IsMismatch ? Colors.Grey.Lighten4 : Colors.White;

                        var safetyColor = r.Safety switch
                        {
                            TagSafety.Safe => Colors.Green.Darken1,
                            TagSafety.Caution => Colors.Orange.Darken1,
                            TagSafety.Unsafe => Colors.Red.Darken1,
                            _ => Colors.Grey.Medium,
                        };

                        var safetyBg = r.Safety switch
                        {
                            TagSafety.Safe => Colors.Green.Lighten4,
                            TagSafety.Caution => Colors.Orange.Lighten4,
                            TagSafety.Unsafe => Colors.Red.Lighten4,
                            _ => Colors.White,
                        };

                        var leftVal = redactPhi ? PhiRedactionService.Redact(r.Tag, r.LeftValue) : r.LeftValue;
                        var rightVal = redactPhi ? PhiRedactionService.Redact(r.Tag, r.RightValue) : r.RightValue;

                        table.Cell().Background(safetyBg).Padding(4)
                            .Text(r.Safety.ToString().ToUpper()).Bold().FontSize(7).FontColor(safetyColor);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(r.Tag).FontSize(7).FontColor(Colors.Blue.Darken2);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(r.TagName).FontSize(7);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(leftVal).FontSize(7).FontColor(Colors.Red.Darken2);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(rightVal).FontSize(7).FontColor(Colors.Green.Darken2);
                        table.Cell().Background(bgColor).Padding(4)
                            .Text(r.Status.ToString()).FontSize(7);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("DiCOMpare Report - Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                    text.Span("  |  For diagnostic purposes only. Not a clinical decision tool.");
                });
            });
        }).GeneratePdf(filePath);
    }

    private static string RedactSummary(string summary)
    {
        // Redact the Patient: line in study summaries
        var lines = summary.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("Patient:"))
                lines[i] = "Patient: [REDACTED]";
        }
        return string.Join('\n', lines);
    }
}
