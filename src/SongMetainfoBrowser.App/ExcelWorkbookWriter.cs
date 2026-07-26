using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SongMetainfoBrowser.App;

internal sealed record ExcelCellLink(
    int Row,
    int Column,
    string TargetSheet,
    int TargetRow,
    int TargetEndRow,
    int TargetEndColumn);

internal sealed class ExcelWorksheetData
{
    public required string Name { get; init; }
    public required IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; }
    public IReadOnlyList<ExcelCellLink> Links { get; init; } = Array.Empty<ExcelCellLink>();
}

internal static class ExcelWorkbookWriter
{
    public static void Write(string path, IReadOnlyList<ExcelWorksheetData> worksheets)
    {
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);

        WriteXmlEntry(archive, "[Content_Types].xml", writer => WriteContentTypes(writer, worksheets.Count));
        WriteXmlEntry(archive, "_rels/.rels", WriteRootRelationships);
        WriteXmlEntry(archive, "xl/workbook.xml", writer => WriteWorkbook(writer, worksheets));
        WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", writer => WriteWorkbookRelationships(writer, worksheets.Count));
        WriteXmlEntry(archive, "xl/styles.xml", WriteStyles);

        for (var index = 0; index < worksheets.Count; index++)
        {
            var sheet = worksheets[index];
            WriteXmlEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", writer => WriteWorksheet(writer, sheet));
        }
    }

    private static void WriteXmlEntry(ZipArchive archive, string path, Action<XmlWriter> write)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });
        write(writer);
    }

    private static void WriteContentTypes(XmlWriter writer, int sheetCount)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
        WriteDefaultType(writer, "rels", "application/vnd.openxmlformats-package.relationships+xml");
        WriteDefaultType(writer, "xml", "application/xml");
        WriteOverrideType(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        WriteOverrideType(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        for (var index = 1; index <= sheetCount; index++)
        {
            WriteOverrideType(writer, $"/xl/worksheets/sheet{index}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteDefaultType(XmlWriter writer, string extension, string contentType)
    {
        writer.WriteStartElement("Default");
        writer.WriteAttributeString("Extension", extension);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteOverrideType(XmlWriter writer, string partName, string contentType)
    {
        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", partName);
        writer.WriteAttributeString("ContentType", contentType);
        writer.WriteEndElement();
    }

    private static void WriteRootRelationships(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        WriteRelationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbook(XmlWriter writer, IReadOnlyList<ExcelWorksheetData> worksheets)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        writer.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
        writer.WriteStartElement("sheets");
        for (var index = 0; index < worksheets.Count; index++)
        {
            writer.WriteStartElement("sheet");
            writer.WriteAttributeString("name", worksheets[index].Name);
            writer.WriteAttributeString("sheetId", (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", $"rId{index + 1}");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbookRelationships(XmlWriter writer, int sheetCount)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
        for (var index = 1; index <= sheetCount; index++)
        {
            WriteRelationship(writer, $"rId{index}", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", $"worksheets/sheet{index}.xml");
        }

        WriteRelationship(writer, $"rId{sheetCount + 1}", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles", "styles.xml");
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRelationship(XmlWriter writer, string id, string type, string target)
    {
        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", id);
        writer.WriteAttributeString("Type", type);
        writer.WriteAttributeString("Target", target);
        writer.WriteEndElement();
    }

    private static void WriteStyles(XmlWriter writer)
    {
        const string spreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", spreadsheetNamespace);

        writer.WriteStartElement("fonts");
        writer.WriteAttributeString("count", "3");
        WriteFont(writer, bold: false, hyperlink: false);
        WriteFont(writer, bold: true, hyperlink: false);
        WriteFont(writer, bold: false, hyperlink: true);
        writer.WriteEndElement();

        writer.WriteStartElement("fills");
        writer.WriteAttributeString("count", "3");
        WritePatternFill(writer, "none", null);
        WritePatternFill(writer, "gray125", null);
        WritePatternFill(writer, "solid", "FFD6E4F0");
        writer.WriteEndElement();

        writer.WriteStartElement("borders");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("border");
        writer.WriteElementString("left", "");
        writer.WriteElementString("right", "");
        writer.WriteElementString("top", "");
        writer.WriteElementString("bottom", "");
        writer.WriteElementString("diagonal", "");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyleXfs");
        writer.WriteAttributeString("count", "1");
        WriteXf(writer, 0, 0, applyFont: false, applyFill: false, wrapText: false);
        writer.WriteEndElement();

        writer.WriteStartElement("cellXfs");
        writer.WriteAttributeString("count", "5");
        WriteXf(writer, 0, 0, applyFont: false, applyFill: false, wrapText: false);
        WriteXf(writer, 1, 2, applyFont: true, applyFill: true, wrapText: false);
        WriteXf(writer, 2, 0, applyFont: true, applyFill: false, wrapText: false);
        WriteXf(writer, 0, 0, applyFont: false, applyFill: false, wrapText: true);
        WriteXf(writer, 2, 0, applyFont: true, applyFill: false, wrapText: true);
        writer.WriteEndElement();

        writer.WriteStartElement("cellStyles");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("cellStyle");
        writer.WriteAttributeString("name", "Normal");
        writer.WriteAttributeString("xfId", "0");
        writer.WriteAttributeString("builtinId", "0");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteFont(XmlWriter writer, bool bold, bool hyperlink)
    {
        writer.WriteStartElement("font");
        if (bold)
        {
            writer.WriteElementString("b", "");
        }

        if (hyperlink)
        {
            writer.WriteElementString("u", "");
            writer.WriteStartElement("color");
            writer.WriteAttributeString("rgb", "FF0563C1");
            writer.WriteEndElement();
        }

        writer.WriteStartElement("sz");
        writer.WriteAttributeString("val", "11");
        writer.WriteEndElement();
        writer.WriteStartElement("name");
        writer.WriteAttributeString("val", "Calibri");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WritePatternFill(XmlWriter writer, string patternType, string? color)
    {
        writer.WriteStartElement("fill");
        writer.WriteStartElement("patternFill");
        writer.WriteAttributeString("patternType", patternType);
        if (color is not null)
        {
            writer.WriteStartElement("fgColor");
            writer.WriteAttributeString("rgb", color);
            writer.WriteEndElement();
            writer.WriteStartElement("bgColor");
            writer.WriteAttributeString("indexed", "64");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteXf(XmlWriter writer, int fontId, int fillId, bool applyFont, bool applyFill, bool wrapText)
    {
        writer.WriteStartElement("xf");
        writer.WriteAttributeString("numFmtId", "0");
        writer.WriteAttributeString("fontId", fontId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fillId", fillId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteAttributeString("borderId", "0");
        writer.WriteAttributeString("xfId", "0");
        if (applyFont)
        {
            writer.WriteAttributeString("applyFont", "1");
        }

        if (applyFill)
        {
            writer.WriteAttributeString("applyFill", "1");
        }

        if (wrapText)
        {
            writer.WriteAttributeString("applyAlignment", "1");
            writer.WriteStartElement("alignment");
            writer.WriteAttributeString("vertical", "top");
            writer.WriteAttributeString("wrapText", "1");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteWorksheet(XmlWriter writer, ExcelWorksheetData sheet)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        var columnCount = Math.Max(1, sheet.Rows.Select(row => row.Count).DefaultIfEmpty(1).Max());
        var rowCount = Math.Max(1, sheet.Rows.Count);
        writer.WriteStartElement("dimension");
        writer.WriteAttributeString("ref", $"A1:{GetColumnName(columnCount)}{rowCount}");
        writer.WriteEndElement();

        writer.WriteStartElement("sheetViews");
        writer.WriteStartElement("sheetView");
        writer.WriteAttributeString("workbookViewId", "0");
        writer.WriteStartElement("pane");
        writer.WriteAttributeString("ySplit", "1");
        writer.WriteAttributeString("topLeftCell", "A2");
        writer.WriteAttributeString("activePane", "bottomLeft");
        writer.WriteAttributeString("state", "frozen");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        var columnWidths = GetColumnWidths(sheet.Rows, columnCount);
        WriteColumns(writer, columnWidths);
        var linksByCell = sheet.Links.ToDictionary(link => (link.Row, link.Column));
        writer.WriteStartElement("sheetData");
        for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
        {
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", (rowIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            var row = sheet.Rows[rowIndex];
            var wrappedLineCount = rowIndex == 0 ? 1 : GetWrappedLineCount(row, columnWidths);
            if (wrappedLineCount > 1)
            {
                var rowHeight = Math.Min(120, 15 * wrappedLineCount);
                writer.WriteAttributeString("ht", rowHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("customHeight", "1");
            }

            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                var cellRow = rowIndex + 1;
                var cellColumn = columnIndex + 1;
                var isLink = linksByCell.ContainsKey((cellRow, cellColumn));
                var requiresWrap = RequiresWrap(row[columnIndex], columnWidths[columnIndex]);
                var styleIndex = cellRow == 1
                    ? 1
                    : (isLink, requiresWrap) switch
                    {
                        (true, true) => 4,
                        (true, false) => 2,
                        (false, true) => 3,
                        _ => 0
                    };
                WriteInlineStringCell(writer, cellRow, cellColumn, row[columnIndex], styleIndex);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteStartElement("autoFilter");
        writer.WriteAttributeString("ref", $"A1:{GetColumnName(columnCount)}{rowCount}");
        writer.WriteEndElement();

        if (sheet.Links.Count > 0)
        {
            writer.WriteStartElement("hyperlinks");
            foreach (var link in sheet.Links)
            {
                writer.WriteStartElement("hyperlink");
                writer.WriteAttributeString("ref", $"{GetColumnName(link.Column)}{link.Row}");
                var targetRange = $"A{link.TargetRow}:{GetColumnName(link.TargetEndColumn)}{link.TargetEndRow}";
                writer.WriteAttributeString(
                    "location",
                    $"'{link.TargetSheet.Replace("'", "''", StringComparison.Ordinal)}'!{targetRange}");
                writer.WriteAttributeString("display", SanitizeText(sheet.Rows[link.Row - 1][link.Column - 1]));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static double[] GetColumnWidths(IReadOnlyList<IReadOnlyList<string?>> rows, int columnCount)
    {
        var widths = new double[columnCount];
        for (var column = 1; column <= columnCount; column++)
        {
            var maxLineLength = rows
                .Select(row => column <= row.Count
                    ? SanitizeText(row[column - 1]).Split('\n').Select(line => line.TrimEnd('\r').Length).DefaultIfEmpty(0).Max()
                    : 0)
                .DefaultIfEmpty(0)
                .Max();
            widths[column - 1] = Math.Clamp(maxLineLength + 2, 10, 80);
        }

        return widths;
    }

    private static void WriteColumns(XmlWriter writer, IReadOnlyList<double> widths)
    {
        writer.WriteStartElement("cols");
        for (var column = 1; column <= widths.Count; column++)
        {
            writer.WriteStartElement("col");
            writer.WriteAttributeString("min", column.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("max", column.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("width", widths[column - 1].ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("customWidth", "1");
            writer.WriteAttributeString("bestFit", "1");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static int GetWrappedLineCount(IReadOnlyList<string?> row, IReadOnlyList<double> columnWidths)
    {
        var lineCount = 1;
        for (var index = 0; index < row.Count; index++)
        {
            var width = Math.Max(1, (int)columnWidths[index] - 2);
            var cellLineCount = SanitizeText(row[index])
                .Split('\n')
                .Sum(line => Math.Max(1, (int)Math.Ceiling(line.TrimEnd('\r').Length / (double)width)));
            lineCount = Math.Max(lineCount, cellLineCount);
        }

        return lineCount;
    }

    private static bool RequiresWrap(string? value, double columnWidth)
    {
        var text = SanitizeText(value);
        return text.Contains('\n') || text.Contains('\r') || text.Length > Math.Max(1, (int)columnWidth - 2);
    }

    private static void WriteInlineStringCell(XmlWriter writer, int row, int column, string? value, int styleIndex)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", $"{GetColumnName(column)}{row}");
        writer.WriteAttributeString("t", "inlineStr");
        if (styleIndex > 0)
        {
            writer.WriteAttributeString("s", styleIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        writer.WriteStartElement("is");
        writer.WriteStartElement("t");
        writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        writer.WriteString(SanitizeText(value));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string SanitizeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var text = new string(value.Where(XmlConvert.IsXmlChar).ToArray());
        return text.Length <= 32767 ? text : text[..32767];
    }

    private static string GetColumnName(int column)
    {
        var name = "";
        while (column > 0)
        {
            column--;
            name = (char)('A' + (column % 26)) + name;
            column /= 26;
        }

        return name;
    }
}
