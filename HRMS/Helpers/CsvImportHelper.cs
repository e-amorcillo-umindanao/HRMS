using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace HRMS.Helpers;

public static class CsvImportHelper
{
    public static async Task<HomeownerCsvParseResult> ParseHomeownersAsync(Stream csvStream)
    {
        if (csvStream.CanSeek)
        {
            csvStream.Position = 0;
        }

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        using var csv = new CsvReader(reader, configuration);
        csv.Context.RegisterClassMap<HomeownerCsvRecordMap>();

        var result = new HomeownerCsvParseResult();

        if (!await csv.ReadAsync())
        {
            return result;
        }

        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var rowNumber = csv.Context.Parser?.Row ?? 0;

            try
            {
                var record = csv.GetRecord<HomeownerCsvRecord>();
                record.RowNumber = rowNumber;
                result.Records.Add(record);
            }
            catch (Exception exception)
            {
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = rowNumber,
                    Message = exception.InnerException?.Message ?? exception.Message
                });
            }
        }

        return result;
    }
}

public sealed class HomeownerCsvRecord
{
    public int RowNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string ResidencySince { get; set; } = string.Empty;
}

public sealed class CsvImportError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class HomeownerCsvParseResult
{
    public List<HomeownerCsvRecord> Records { get; } = [];
    public List<CsvImportError> Errors { get; } = [];
}

public sealed class HomeownerImportResult
{
    public int ImportedCount { get; set; }
    public List<CsvImportError> Errors { get; } = [];
}

public sealed class HomeownerCsvRecordMap : ClassMap<HomeownerCsvRecord>
{
    public HomeownerCsvRecordMap()
    {
        Map(m => m.FirstName).Name("FirstName");
        Map(m => m.MiddleName).Name("MiddleName");
        Map(m => m.LastName).Name("LastName");
        Map(m => m.BirthDate).Name("BirthDate");
        Map(m => m.Gender).Name("Gender");
        Map(m => m.ContactNumber).Name("ContactNumber");
        Map(m => m.Email).Name("Email");
        Map(m => m.Status).Name("Status");
        Map(m => m.ResidencySince).Name("ResidencySince");
    }
}
