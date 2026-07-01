using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace PztGenerator;

public static class PztDocxExporter
{
    public static void ExportBalance(UrbanReport report, string buildText, string filePath)
    {
        var body = new StringBuilder();

        body.Append(Paragraph("Bilans terenu PZT", ParagraphKind.Title));
        body.Append(Paragraph($"Data opracowania: {DateTime.Now:yyyy-MM-dd}", ParagraphKind.Meta));
        body.Append(Paragraph("Zestawienie testowe wygenerowane z modelu Revit.", ParagraphKind.Meta));

        body.Append(Paragraph("Podstawowe wskazniki", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Lp.", "Wskaznik", "Wartosc" },
            new[]
            {
                new[] { "1", "Powierzchnia dzialki", SquareMeters(report.SiteAreaSquareMeters) },
                new[] { "2", "Powierzchnia zabudowy", $"{SquareMeters(report.BuildingFootprintSquareMeters)} ({Percent(report.BuildingCoveragePercent)} pow. dzialki)" },
                new[] { "3", "Powierzchnia utwardzona", SquareMeters(report.HardenedAreaSquareMeters) },
                new[] { "4", "Powierzchnia biologicznie czynna", $"{SquareMeters(report.BioAreaSquareMeters)} ({Percent(report.BioPercent)} pow. dzialki)" },
                new[] { "5", "Powierzchnia calkowita", SquareMeters(report.GrossFloorAreaSquareMeters) },
                new[] { "6", "Intensywnosc zabudowy", Number(report.Intensity) },
                new[] { "7", "Miejsca parkingowe", $"Razem: {report.ParkingSpaceCount:N0}, zwykle: {report.RegularParkingSpaceCount:N0}, N: {report.AccessibleParkingSpaceCount:N0}" }
            }));

        body.Append(Paragraph("Bilans powierzchni wedlug typu i stanu", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Kategoria", "Stan", "Szt.", "Powierzchnia", "Udzial dzialki", "Informacje" },
            report.Rows.Select(row =>
            {
                var share = report.SiteAreaSquareMeters > 0
                    ? Percent(row.AreaSquareMeters / report.SiteAreaSquareMeters * 100)
                    : "-";

                return new[]
                {
                    row.Category,
                    FormatState(row.Status),
                    row.AreaCount.ToString(CultureInfo.CurrentCulture),
                    SquareMeters(row.AreaSquareMeters),
                    share,
                    BuildDetails(row)
                };
            })));

        body.Append(Paragraph("Walidacja MPZP", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Warunek / rachunek", "Wynik" },
            report.ValidationMessages.Select(message => new[]
            {
                message.Text,
                FormatValidationResult(message.Severity)
            })));

        body.Append(Paragraph(buildText, ParagraphKind.Footer));

        Save(filePath, body.ToString());
    }

    public static void ExportMpzpValidation(UrbanReport report, string buildText, string filePath)
    {
        var body = new StringBuilder();

        body.Append(Paragraph("Walidacja warunkow MPZP", ParagraphKind.Title));
        body.Append(Paragraph($"Data opracowania: {DateTime.Now:yyyy-MM-dd}", ParagraphKind.Meta));
        body.Append(Paragraph($"Powierzchnia dzialki: {SquareMeters(report.SiteAreaSquareMeters)}", ParagraphKind.Meta));

        body.Append(Table(
            new[] { "Lp.", "Warunek / rachunek", "Wynik" },
            report.ValidationMessages.Select((message, index) => new[]
            {
                (index + 1).ToString(CultureInfo.CurrentCulture),
                message.Text,
                FormatValidationResult(message.Severity)
            })));

        body.Append(Paragraph(buildText, ParagraphKind.Footer));

        Save(filePath, body.ToString());
    }

    private static void Save(string filePath, string bodyXml)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using FileStream stream = File.Create(filePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        WriteEntry(archive, "[Content_Types].xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
            "</Types>");

        WriteEntry(archive, "_rels/.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
            "</Relationships>");

        WriteEntry(archive, "word/document.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
            "<w:body>" +
            bodyXml +
            "<w:sectPr>" +
            "<w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"850\" w:right=\"850\" w:bottom=\"850\" w:left=\"850\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/>" +
            "</w:sectPr>" +
            "</w:body>" +
            "</w:document>");
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Paragraph(string text, ParagraphKind kind)
    {
        var size = kind switch
        {
            ParagraphKind.Title => 24,
            ParagraphKind.Heading => 20,
            ParagraphKind.Footer => 14,
            _ => 18
        };

        var after = kind switch
        {
            ParagraphKind.Title => 120,
            ParagraphKind.Heading => 80,
            ParagraphKind.Footer => 0,
            _ => 20
        };

        var before = kind == ParagraphKind.Heading ? 160 : 0;
        var bold = kind is ParagraphKind.Title or ParagraphKind.Heading ? "<w:b/>" : string.Empty;
        var color = kind == ParagraphKind.Footer ? "<w:color w:val=\"777777\"/>" : string.Empty;

        return
            "<w:p>" +
            "<w:pPr><w:spacing w:before=\"" + before + "\" w:after=\"" + after + "\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr>" +
            "<w:r><w:rPr>" + bold + color + "<w:sz w:val=\"" + size + "\"/></w:rPr><w:t>" + Escape(text) + "</w:t></w:r>" +
            "</w:p>";
    }

    private static string Table(string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();

        builder.Append(
            "<w:tbl>" +
            "<w:tblPr>" +
            "<w:tblW w:w=\"5000\" w:type=\"pct\"/>" +
            "<w:tblLayout w:type=\"autofit\"/>" +
            "<w:tblBorders>" +
            "<w:top w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"8A8A8A\"/>" +
            "<w:left w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"8A8A8A\"/>" +
            "<w:bottom w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"8A8A8A\"/>" +
            "<w:right w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"8A8A8A\"/>" +
            "<w:insideH w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"D0D0D0\"/>" +
            "<w:insideV w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"D0D0D0\"/>" +
            "</w:tblBorders>" +
            "<w:tblCellMar>" +
            "<w:top w:w=\"70\" w:type=\"dxa\"/><w:left w:w=\"70\" w:type=\"dxa\"/>" +
            "<w:bottom w:w=\"70\" w:type=\"dxa\"/><w:right w:w=\"70\" w:type=\"dxa\"/>" +
            "</w:tblCellMar>" +
            "</w:tblPr>");

        builder.Append(Row(headers, header: true));

        foreach (string[] row in rows)
        {
            builder.Append(Row(row, header: false));
        }

        builder.Append("</w:tbl>");
        return builder.ToString();
    }

    private static string Row(IEnumerable<string> cells, bool header)
    {
        var builder = new StringBuilder();

        builder.Append("<w:tr>");

        foreach (string cell in cells)
        {
            var shading = header ? "<w:shd w:fill=\"EDEDED\"/>" : string.Empty;
            var bold = header ? "<w:b/>" : string.Empty;

            builder.Append(
                "<w:tc>" +
                "<w:tcPr>" + shading + "<w:vAlign w:val=\"center\"/></w:tcPr>" +
                "<w:p><w:pPr><w:spacing w:before=\"0\" w:after=\"0\" w:line=\"220\" w:lineRule=\"auto\"/></w:pPr>" +
                "<w:r><w:rPr>" + bold + "<w:sz w:val=\"18\"/></w:rPr><w:t>" + Escape(cell) + "</w:t></w:r>" +
                "</w:p>" +
                "</w:tc>");
        }

        builder.Append("</w:tr>");
        return builder.ToString();
    }

    private static string BuildDetails(AreaBalanceRow row)
    {
        var parts = new List<string>();

        if (row.GrossFloorAreaSquareMeters > 0)
        {
            parts.Add($"pow. calk.: {SquareMeters(row.GrossFloorAreaSquareMeters)}");
        }

        if (!string.IsNullOrWhiteSpace(row.BioFactorLabel))
        {
            parts.Add($"wsp. PBC: {row.BioFactorLabel}");
        }

        if (row.BioAreaSquareMeters > 0)
        {
            parts.Add($"PBC: {SquareMeters(row.BioAreaSquareMeters)}");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }

    private static string FormatState(string state)
    {
        return string.IsNullOrWhiteSpace(state) ? "-" : state;
    }

    private static string FormatValidationResult(ValidationSeverity severity)
    {
        return severity switch
        {
            ValidationSeverity.Success => "Spelniony",
            ValidationSeverity.Error => "Niespelniony",
            ValidationSeverity.Warning => "Uwaga",
            _ => "Informacja"
        };
    }

    private static string SquareMeters(double value)
    {
        return $"{Number(value)} m2";
    }

    private static string Percent(double value)
    {
        return $"{Number(value)}%";
    }

    private static string Number(double value)
    {
        return value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private static string Escape(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private enum ParagraphKind
    {
        Title,
        Heading,
        Meta,
        Footer
    }
}
