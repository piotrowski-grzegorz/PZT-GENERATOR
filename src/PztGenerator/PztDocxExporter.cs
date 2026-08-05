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
        body.Append(Paragraph("Zestawienie powierzchni do wykorzystania w czesci opisowej projektu architektoniczno-budowlanego.", ParagraphKind.Meta));

        body.Append(Paragraph("1. Podstawowe wskazniki zagospodarowania terenu", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Lp.", "Wskaznik", "Wartosc" },
            new[] { 650, 4700, 3600 },
            BuildIndicatorRows(report)));

        body.Append(Paragraph("2. Bilans powierzchni terenu", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Lp.", "Element bilansu", "Stan", "Liczba", "Powierzchnia [m2]", "Udzial w dzialce [%]", "Uwagi" },
            new[] { 500, 2600, 1300, 850, 1500, 1400, 2300 },
            BuildAreaRows(report)));

        body.Append(Paragraph("3. Sprawdzenie wymagan MPZP", ParagraphKind.Heading));
        body.Append(Table(
            new[] { "Lp.", "Warunek / rachunek", "Status" },
            new[] { 500, 7000, 1800 },
            BuildValidationRows(report)));

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
            new[] { "Lp.", "Warunek / rachunek", "Status" },
            new[] { 500, 7000, 1800 },
            BuildValidationRows(report)));

        body.Append(Paragraph(buildText, ParagraphKind.Footer));

        Save(filePath, body.ToString());
    }

    private static IEnumerable<TableRow> BuildIndicatorRows(UrbanReport report)
    {
        return new[]
        {
            Row("1", "Powierzchnia terenu / dzialki", SquareMeters(report.SiteAreaSquareMeters)),
            Row("2", "Powierzchnia zabudowy", $"{SquareMeters(report.BuildingFootprintSquareMeters)} ({Percent(report.BuildingCoveragePercent)} pow. dzialki)"),
            Row("3", "Powierzchnia utwardzona", SquareMeters(report.HardenedAreaSquareMeters)),
            Row("4", "Powierzchnia biologicznie czynna", $"{SquareMeters(report.BioAreaSquareMeters)} ({Percent(report.BioPercent)} pow. dzialki)"),
            Row("5", "Powierzchnia calkowita budynkow", SquareMeters(report.GrossFloorAreaSquareMeters)),
            Row("6", "Wskaznik intensywnosci zabudowy", Number(report.Intensity)),
            Row("7", "Miejsca postojowe", $"Razem: {report.ParkingSpaceCount:N0}; standardowe: {report.RegularParkingSpaceCount:N0}; dla osob z niepelnosprawnosciami: {report.AccessibleParkingSpaceCount:N0}")
        };
    }

    private static IEnumerable<TableRow> BuildAreaRows(UrbanReport report)
    {
        var rows = new List<TableRow>();
        int index = 1;

        foreach (IGrouping<string, AreaBalanceRow> statusGroup in report.Rows
            .GroupBy(row => NormalizeStateGroup(row.Status))
            .OrderBy(group => StateOrder(group.Key)))
        {
            string stateLabel = FormatStateGroup(statusGroup.Key);
            rows.Add(new TableRow(
                new[] { "", stateLabel, "", "", "", "", "" },
                "EEF4F6",
                true));

            foreach (AreaBalanceRow row in statusGroup
                .OrderBy(row => CategoryOrder(row.Category))
                .ThenBy(row => row.Category))
            {
                rows.Add(Row(
                    (index++).ToString(CultureInfo.CurrentCulture),
                    FormatCategory(row.Category),
                    FormatState(row.Status),
                    row.AreaCount.ToString(CultureInfo.CurrentCulture),
                    Number(row.AreaSquareMeters),
                    ShareOfSite(row.AreaSquareMeters, report.SiteAreaSquareMeters),
                    BuildDetails(row)));
            }

            rows.Add(new TableRow(
                new[]
                {
                    "",
                    $"Suma - {stateLabel.ToLowerInvariant()}",
                    "",
                    statusGroup.Sum(row => row.AreaCount).ToString(CultureInfo.CurrentCulture),
                    Number(statusGroup.Sum(row => row.AreaSquareMeters)),
                    ShareOfSite(statusGroup.Sum(row => row.AreaSquareMeters), report.SiteAreaSquareMeters),
                    ""
                },
                "EAF3F2",
                true));
        }

        rows.Add(new TableRow(
            new[]
            {
                "",
                "Razem projektowane i istniejace",
                "",
                report.Rows.Sum(row => row.AreaCount).ToString(CultureInfo.CurrentCulture),
                Number(report.TotalAreaSquareMeters),
                ShareOfSite(report.TotalAreaSquareMeters, report.SiteAreaSquareMeters),
                ""
            },
            "D9E8E7",
            true));

        return rows;
    }

    private static IEnumerable<TableRow> BuildValidationRows(UrbanReport report)
    {
        return report.ValidationMessages.Select((message, index) =>
        {
            string fill = message.Severity switch
            {
                ValidationSeverity.Success => "DCEEDC",
                ValidationSeverity.Error => "F4D6D2",
                ValidationSeverity.Warning => "FFF1CC",
                _ => "FFFFFF"
            };

            return new TableRow(
                new[]
                {
                    (index + 1).ToString(CultureInfo.CurrentCulture),
                    message.Text,
                    FormatValidationResult(message.Severity)
                },
                fill,
                message.Severity is ValidationSeverity.Success or ValidationSeverity.Error);
        });
    }

    private static TableRow Row(params string[] cells)
    {
        return new TableRow(cells, null, false);
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
            "<w:pgMar w:top=\"850\" w:right=\"700\" w:bottom=\"850\" w:left=\"700\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/>" +
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
            ParagraphKind.Title => 26,
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

        var before = kind == ParagraphKind.Heading ? 180 : 0;
        var bold = kind is ParagraphKind.Title or ParagraphKind.Heading ? "<w:b/>" : string.Empty;
        var color = kind == ParagraphKind.Footer ? "<w:color w:val=\"777777\"/>" : string.Empty;

        return
            "<w:p>" +
            "<w:pPr><w:spacing w:before=\"" + before + "\" w:after=\"" + after + "\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr>" +
            "<w:r><w:rPr><w:rFonts w:ascii=\"Aptos\" w:hAnsi=\"Aptos\"/>" + bold + color + "<w:sz w:val=\"" + size + "\"/></w:rPr><w:t>" + Escape(text) + "</w:t></w:r>" +
            "</w:p>";
    }

    private static string Table(string[] headers, int[] widths, IEnumerable<TableRow> rows)
    {
        var builder = new StringBuilder();

        builder.Append(
            "<w:tbl>" +
            "<w:tblPr>" +
            "<w:tblW w:w=\"5000\" w:type=\"pct\"/>" +
            "<w:tblLayout w:type=\"fixed\"/>" +
            "<w:tblBorders>" +
            "<w:top w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"6B7A80\"/>" +
            "<w:left w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"6B7A80\"/>" +
            "<w:bottom w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"6B7A80\"/>" +
            "<w:right w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"6B7A80\"/>" +
            "<w:insideH w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"D2D9DD\"/>" +
            "<w:insideV w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"D2D9DD\"/>" +
            "</w:tblBorders>" +
            "<w:tblCellMar>" +
            "<w:top w:w=\"80\" w:type=\"dxa\"/><w:left w:w=\"80\" w:type=\"dxa\"/>" +
            "<w:bottom w:w=\"80\" w:type=\"dxa\"/><w:right w:w=\"80\" w:type=\"dxa\"/>" +
            "</w:tblCellMar>" +
            "</w:tblPr>");

        builder.Append(RowXml(new TableRow(headers, "D9E8E7", true), widths, header: true));

        foreach (TableRow row in rows)
        {
            builder.Append(RowXml(row, widths, header: false));
        }

        builder.Append("</w:tbl>");
        return builder.ToString();
    }

    private static string RowXml(TableRow row, int[] widths, bool header)
    {
        var builder = new StringBuilder();
        builder.Append("<w:tr>");

        for (int index = 0; index < row.Cells.Length; index++)
        {
            string cell = row.Cells[index];
            int width = index < widths.Length ? widths[index] : 1500;
            string fill = row.Fill ?? (header ? "D9E8E7" : "FFFFFF");
            string shading = "<w:shd w:fill=\"" + fill + "\"/>";
            string bold = header || row.Bold ? "<w:b/>" : string.Empty;
            string align = IsNumericColumn(index, row.Cells.Length) ? "<w:jc w:val=\"right\"/>" : string.Empty;

            builder.Append(
                "<w:tc>" +
                "<w:tcPr><w:tcW w:w=\"" + width + "\" w:type=\"dxa\"/>" + shading + "<w:vAlign w:val=\"center\"/></w:tcPr>" +
                "<w:p><w:pPr>" + align + "<w:spacing w:before=\"0\" w:after=\"0\" w:line=\"220\" w:lineRule=\"auto\"/></w:pPr>" +
                "<w:r><w:rPr><w:rFonts w:ascii=\"Aptos\" w:hAnsi=\"Aptos\"/>" + bold + "<w:sz w:val=\"17\"/></w:rPr><w:t>" + Escape(cell) + "</w:t></w:r>" +
                "</w:p>" +
                "</w:tc>");
        }

        builder.Append("</w:tr>");
        return builder.ToString();
    }

    private static bool IsNumericColumn(int index, int columnCount)
    {
        if (index == 0)
        {
            return true;
        }

        return columnCount switch
        {
            3 => index == 2,
            7 => index is 3 or 4 or 5,
            _ => false
        };
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

        return parts.Count == 0 ? "-" : string.Join("; ", parts);
    }

    private static int CategoryOrder(string category)
    {
        if (string.Equals(category, PztCategories.SiteBoundary, StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(category, PztCategories.Building, StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(category, PztCategories.AccessRoad, StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(category, PztCategories.GroundSurface, StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(category, PztCategories.Walkway, StringComparison.OrdinalIgnoreCase)) return 4;
        if (string.Equals(category, PztCategories.Square, StringComparison.OrdinalIgnoreCase)) return 5;
        if (string.Equals(category, PztCategories.TerrainStairs, StringComparison.OrdinalIgnoreCase)) return 6;
        if (string.Equals(category, PztCategories.Parking, StringComparison.OrdinalIgnoreCase)) return 7;
        if (string.Equals(category, PztCategories.BioActive, StringComparison.OrdinalIgnoreCase)) return 8;
        if (string.Equals(category, PztCategories.SemiPermeable, StringComparison.OrdinalIgnoreCase)) return 9;
        return 99;
    }

    private static string FormatCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category) ? "-" : category;
    }

    private static string FormatState(string state)
    {
        return string.IsNullOrWhiteSpace(state) ? "-" : state;
    }

    private static string NormalizeStateGroup(string state)
    {
        if (string.Equals(state, "Projektowana", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "Projektowane", StringComparison.OrdinalIgnoreCase))
        {
            return "Projektowane";
        }

        if (string.Equals(state, "Istniejaca", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "Istniejace", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "IstniejÄ…ca", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(state, "IstniejÄ…ce", StringComparison.OrdinalIgnoreCase))
        {
            return "Istniejace";
        }

        return string.IsNullOrWhiteSpace(state) ? "Bez okreslonego stanu" : state.Trim();
    }

    private static string FormatStateGroup(string state)
    {
        return state switch
        {
            "Projektowane" => "Elementy projektowane",
            "Istniejace" => "Elementy istniejace",
            "Bez okreslonego stanu" => "Elementy bez okreslonego stanu",
            _ => $"Elementy: {state}"
        };
    }

    private static int StateOrder(string state)
    {
        return state switch
        {
            "Projektowane" => 0,
            "Istniejace" => 1,
            "Bez okreslonego stanu" => 2,
            _ => 3
        };
    }

    private static string ShareOfSite(double area, double siteArea)
    {
        return siteArea > 0 ? Percent(area / siteArea * 100).Replace("%", string.Empty) : "-";
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

    private sealed record TableRow(string[] Cells, string? Fill, bool Bold);

    private enum ParagraphKind
    {
        Title,
        Heading,
        Meta,
        Footer
    }
}
