using ClosedXML.Excel;

namespace HRMS.Helpers;

public static class ExcelExportHelper
{
    public static byte[] GenerateWorksheet(
        string worksheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(SanitizeWorksheetName(worksheetName));

        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.FromHtml("1E293B");
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("EEF2FF");
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.BottomBorderColor = XLColor.FromHtml("E2E8F0");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];

            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                var value = columnIndex < row.Count ? row[columnIndex] : "-";
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = value;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string SanitizeWorksheetName(string worksheetName)
    {
        if (string.IsNullOrWhiteSpace(worksheetName))
        {
            return "Report";
        }

        var invalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars().Concat(['[', ']', '*', '?', '/', '\\']));
        var sanitized = new string(worksheetName
            .Where(character => !invalidCharacters.Contains(character))
            .ToArray());

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Report";
        }

        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}
