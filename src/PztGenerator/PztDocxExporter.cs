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

        body.Append(Paragraph("Bilans obszarow PZT", bold: true, size: 32));
        body.Append(Paragraph($"{DateTime.Now:yyyy-MM-dd HH:mm}"));
        body.Append(Paragraph(buildText));
        body.Append(Paragraph("MVP / prototyp testowy - raport sluzy do sprawdzenia workflow i logiki bilansu."));

        body.Append(Paragraph("Wskazniki", bold: true, size: 24));
        body.Append(Table(
            new[] { "Wskaznik", "Wartosc" },
            new[]
            {
                new[] { "Powierzchnia dzialki", SquareMeters(report.SiteAreaSquareMeters) },
                new[] { "Powierzchnia zabudowy", $"{SquareMeters(report.BuildingFootprintSquareMeters)} ({Percent(report.BuildingCoveragePercent)} pow. dzialki)" },
                new[] { "Powierzchnia utwardzona", SquareMeters(report.HardenedAreaSquareMeters) },
                new[] { "Powierzchnia biologicznie czynna", $"{SquareMeters(report.BioAreaSquareMeters)} ({Percent(report.BioPercent)} pow. dzialki)" },
                new[] { "Powierzchnia calkowita", SquareMeters(report.GrossFloorAreaSquareMeters) },
                new[] { "Intensywnosc zabudowy", Number(report.Intensity) },
                new[] { "Miejsca parkingowe", $"Razem: {report.ParkingSpaceCount:N0}, zwykle: {report.RegularParkingSpaceCount:N0}, N: {report.AccessibleParkingSpaceCount:N0}" }
            }));

        body.Append(Paragraph("Bilans typow PZT", bold: true, size: 24));
        body.Append(Table(
            new[] { "Kategoria", "Status", "Szt.", "Powierzchnia", "Udzial dzialki", "Informacje" },
            report.Rows.Select(row =>
            {
                var share = report.SiteAreaSquareMeters > 0
                    ? Percent(row.AreaSquareMeters / report.SiteAreaSquareMeters * 100)
                    : "-";

                return new[]
                {
                    row.Category,
                    string.IsNullOrWhiteSpace(row.Status) ? "-" : row.Status,
                    row.AreaCount.ToString(CultureInfo.CurrentCulture),
                    SquareMeters(row.AreaSquareMeters),
                    share,
                    BuildDetails(row)
                };
            })));

        body.Append(Paragraph("Walidacja MPZP", bold: true, size: 24));
        body.Append(Table(
            new[] { "Warunek", "Status" },
            report.ValidationMessages.Select(message => new[]
            {
                message.Text,
                FormatStatus(message.Severity)
            })));

        Save(filePath, body.ToString());
    }

    public static void ExportMpzpValidation(UrbanReport report, string buildText, string filePath)
    {
        var body = new StringBuilder();

        body.Append(Paragraph("Walidacja MPZP", bold: true, size: 32));
        body.Append(Paragraph($"{DateTime.Now:yyyy-MM-dd HH:mm}"));
        body.Append(Paragraph(buildText));
        body.Append(Paragraph($"Powierzchnia dzialki: {SquareMeters(report.SiteAreaSquareMeters)}"));

        body.Append(Table(
            new[] { "Warunek", "Status" },
            report.ValidationMessages.Select(message => new[]
            {
                message.Text,
                FormatStatus(message.Severity)
            })));

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
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>
            """);

        WriteEntry(archive, "_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
            </Relationships>
            """);

        WriteEntry(archive, "word/document.xml",
            $$"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                {{bodyXml}}
                <w:sectPr>
                  <w:pgSz w:w="11906" w:h="16838"/>
                  <w:pgMar w:top="1134" w:right="850" w:bottom="1134" w:left="850" w:header="708" w:footer="708" w:gutter="0"/>
                </w:sectPr>
              </w:body>
            </w:document>
            """);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Paragraph(string text, bool bold = false, int size = 22)
    {
        var boldXml = bold ? "<w:b/>" : string.Empty;

        return $$"""
        <w:p>
          <w:r>
            <w:rPr>{{boldXml}}<w:sz w:val="{{size}}"/></w:rPr>
            <w:t>{{Escape(text)}}</w:t>
          </w:r>
        </w:p>
        """;
    }

    private static string Table(string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();

        builder.Append("""
        <w:tbl>
          <w:tblPr>
            <w:tblBorders>
              <w:top w:val="single" w:sz="4" w:space="0" w:color="B7B7B7"/>
              <w:left w:val="single" w:sz="4" w:space="0" w:color="B7B7B7"/>
              <w:bottom w:val="single" w:sz="4" w:space="0" w:color="B7B7B7"/>
              <w:right w:val="single" w:sz="4" w:space="0" w:color="B7B7B7"/>
              <w:insideH w:val="single" w:sz="4" w:space="0" w:color="D9D9D9"/>
              <w:insideV w:val="single" w:sz="4" w:space="0" w:color="D9D9D9"/>
            </w:tblBorders>
            <w:tblCellMar>
              <w:top w:w="80" w:type="dxa"/><w:left w:w="80" w:type="dxa"/>
              <w:bottom w:w="80" w:type="dxa"/><w:right w:w="80" w:type="dxa"/>
            </w:tblCellMar>
          </w:tblPr>
        """);

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
            var shading = header ? """<w:shd w:fill="EDEDED"/>""" : string.Empty;
            var bold = header ? "<w:b/>" : string.Empty;

            builder.Append($$"""
            <w:tc>
              <w:tcPr>{{shading}}</w:tcPr>
              <w:p>
                <w:r>
                  <w:rPr>{{bold}}<w:sz w:val="20"/></w:rPr>
                  <w:t>{{Escape(cell)}}</w:t>
                </w:r>
              </w:p>
            </w:tc>
            """);
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
            parts.Add($"wsp. bio: {row.BioFactorLabel}");
        }

        if (row.BioAreaSquareMeters > 0)
        {
            parts.Add($"PBC: {SquareMeters(row.BioAreaSquareMeters)}");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }

    private static string FormatStatus(ValidationSeverity severity)
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
}
