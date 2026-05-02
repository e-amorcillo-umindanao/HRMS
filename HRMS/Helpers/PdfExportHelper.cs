using HRMS.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRMS.Helpers;

public static class PdfExportHelper
{
    public static byte[] GenerateTableReport(
        HOASettings? settings,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeTableReport(content, title, headers, rows));
            page.Footer().AlignCenter().Text($"Generated {DateTime.UtcNow:MMMM dd, yyyy}");
        });
    }

    public static byte[] GenerateDuesStatement(HOASettings? settings, Homeowner homeowner, IReadOnlyCollection<DuesRecord> duesRecords)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeDuesStatement(content, settings, homeowner, duesRecords));
            page.Footer().AlignCenter().Text($"Generated {DateTime.UtcNow:MMMM dd, yyyy}");
        });
    }

    public static byte[] GenerateHoaClearance(HOASettings? settings, ClearanceRequest request)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeHoaClearance(content, settings, request));
            page.Footer().AlignCenter().Text($"President: {DisplayValue(settings?.PresidentName)}");
        });
    }

    public static byte[] GenerateCertificateOfResidency(HOASettings? settings, Homeowner homeowner)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeResidencyCertificate(content, homeowner));
            page.Footer().AlignCenter().Text($"Secretary: {DisplayValue(settings?.SecretaryName)}");
        });
    }

    public static byte[] GenerateCertificateOfGoodStanding(HOASettings? settings, Homeowner homeowner)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeGoodStandingCertificate(content, homeowner));
            page.Footer().AlignCenter().Text($"President: {DisplayValue(settings?.PresidentName)}");
        });
    }

    public static byte[] GenerateOfficialLetter(HOASettings? settings, Homeowner homeowner, string purpose)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeOfficialLetter(content, settings, homeowner, purpose));
            page.Footer().AlignCenter().Text($"Prepared by {DisplayValue(settings?.PresidentName)}");
        });
    }

    public static byte[] GenerateViolationReport(HOASettings? settings, ViolationRecord violation)
    {
        return CreatePdf(page =>
        {
            page.Header().Element(header => ComposeHeader(header, settings));
            page.Content().Element(content => ComposeViolationReport(content, violation));
            page.Footer().AlignCenter().Text($"Generated {DateTime.UtcNow:MMMM dd, yyyy}");
        });
    }

    private static byte[] CreatePdf(Action<PageDescriptor> configurePage)
    {
        using var stream = new MemoryStream();

        Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.DefaultTextStyle(style => style.FontSize(10).FontColor("#1E293B"));
                    configurePage(page);
                });
            })
            .GeneratePdf(stream);

        return stream.ToArray();
    }

    private static void ComposeHeader(QuestPDF.Infrastructure.IContainer container, HOASettings? settings)
    {
        container.Row(row =>
        {
            var logoBytes = TryReadLogo(settings?.LogoPath);
            if (logoBytes is not null)
            {
                row.ConstantItem(60)
                    .Height(60)
                    .Image(logoBytes);
            }

            row.RelativeItem().Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(settings?.HOAName ?? "Homeowners Association").Bold().FontSize(16).FontColor("#1E293B");
                column.Item().Text(GetLocationLine(settings)).FontColor("#64748B");
                if (!string.IsNullOrWhiteSpace(settings?.ContactNumber))
                {
                    column.Item().Text($"Contact: {settings.ContactNumber}").FontColor("#64748B");
                }
            });
        });
    }

    private static void ComposeDuesStatement(QuestPDF.Infrastructure.IContainer container, HOASettings? settings, Homeowner homeowner, IReadOnlyCollection<DuesRecord> duesRecords)
    {
        var totalDue = duesRecords
            .Where(record => record.Status is "Unpaid" or "Overdue")
            .Sum(record => record.Amount);

        container.Column(column =>
        {
            column.Spacing(16);

            column.Item().PaddingTop(12).Text("Dues Statement").Bold().FontSize(14);

            column.Item().Border(1).BorderColor("#E2E8F0").Padding(12).Column(info =>
            {
                info.Spacing(6);
                info.Item().Text($"Homeowner: {GetFullName(homeowner)}");
                info.Item().Text($"Unit: {GetUnitDisplay(homeowner)}");
                info.Item().Text($"HOA: {settings?.HOAName ?? "Homeowners Association"}");
            });

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Month"));
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Year"));
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Amount"));
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Due Date"));
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Status"));
                    header.Cell().Element(cell => StyleHeaderCell(cell, "Paid Date"));
                });

                foreach (var dues in duesRecords.OrderByDescending(record => record.Year).ThenByDescending(record => record.Month))
                {
                    table.Cell().Element(cell => StyleBodyCell(cell, GetMonthName(dues.Month)));
                    table.Cell().Element(cell => StyleBodyCell(cell, dues.Year.ToString()));
                    table.Cell().Element(cell => StyleBodyCell(cell, FormatHelper.Peso(dues.Amount)));
                    table.Cell().Element(cell => StyleBodyCell(cell, FormatDate(dues.DueDate)));
                    table.Cell().Element(cell => StyleBodyCell(cell, dues.Status));
                    table.Cell().Element(cell => StyleBodyCell(cell, FormatDate(dues.PaidDate)));
                }

                if (duesRecords.Count == 0)
                {
                    table.Cell().ColumnSpan(6).Border(1).BorderColor("#E2E8F0").Padding(8).Text("No dues records available.");
                }
            });

            column.Item().AlignRight().Text($"Total Amount Due: {FormatHelper.Peso(totalDue)}").Bold().FontSize(12);
        });
    }

    private static void ComposeHoaClearance(QuestPDF.Infrastructure.IContainer container, HOASettings? settings, ClearanceRequest request)
    {
        var homeowner = request.Homeowner;
        var validUntil = FormatDate(request.ValidUntil);

        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(14);

            column.Item().Text("HOA Clearance").Bold().FontSize(15);
            column.Item().Text($"Date Approved: {FormatDate(request.ProcessedAt)}");

            column.Item().Text(text =>
            {
                text.Span("This is to certify that ");
                text.Span(GetFullName(homeowner)).Bold();
                text.Span(", residing at ");
                text.Span(GetUnitDisplay(homeowner)).Bold();
                text.Span(", has been granted ");
                text.Span(request.ClearanceType).Bold();
                text.Span(" for the purpose of ");
                text.Span(request.Purpose).Bold();
                text.Span(".");
            });

            column.Item().Text($"Valid Until: {validUntil}");
            column.Item().Text($"Remarks: {DisplayValue(request.Remarks)}");

            column.Item().PaddingTop(30).AlignRight().Column(signature =>
            {
                signature.Item().LineHorizontal(1);
                signature.Item().Text(DisplayValue(settings?.PresidentName)).Bold();
                signature.Item().Text("HOA President").FontColor("#64748B");
            });
        });
    }

    private static void ComposeResidencyCertificate(QuestPDF.Infrastructure.IContainer container, Homeowner homeowner)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(14);

            column.Item().Text("Certificate of Residency").Bold().FontSize(15);
            column.Item().Text(text =>
            {
                text.Span("This certifies that ");
                text.Span(GetFullName(homeowner)).Bold();
                text.Span(" is a registered homeowner residing in the subdivision since ");
                text.Span(FormatDate(homeowner.ResidencySince)).Bold();
                text.Span(".");
            });
        });
    }

    private static void ComposeGoodStandingCertificate(QuestPDF.Infrastructure.IContainer container, Homeowner homeowner)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(14);

            column.Item().Text("Certificate of Good Standing").Bold().FontSize(15);
            column.Item().Text(text =>
            {
                text.Span("This certifies that ");
                text.Span(GetFullName(homeowner)).Bold();
                text.Span(" is in good standing with the homeowners association and has no outstanding dues or unresolved violations as of ");
                text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy")).Bold();
                text.Span(".");
            });
        });
    }

    private static void ComposeOfficialLetter(QuestPDF.Infrastructure.IContainer container, HOASettings? settings, Homeowner homeowner, string purpose)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(12);

            column.Item().Text($"Date: {DateTime.UtcNow:MMMM dd, yyyy}");
            column.Item().Text($"To: {GetFullName(homeowner)}").Bold();
            column.Item().Text($"Subdivision Resident, {DisplayValue(settings?.Subdivision)}");

            column.Item().PaddingTop(8).Text("Subject: Official Letter").Bold();
            column.Item().Text(text =>
            {
                text.Span("This letter is issued to ");
                text.Span(GetFullName(homeowner)).Bold();
                text.Span(" for ");
                text.Span(purpose).Bold();
                text.Span(". This document is provided by the homeowners association for official reference and coordination.");
            });

            column.Item().PaddingTop(24).Text("Respectfully,");
            column.Item().Text(DisplayValue(settings?.PresidentName)).Bold();
            column.Item().Text("HOA President").FontColor("#64748B");
        });
    }

    private static void ComposeViolationReport(QuestPDF.Infrastructure.IContainer container, ViolationRecord violation)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(12);

            column.Item().Text("Violation Report").Bold().FontSize(15);
            column.Item().Text($"Violation Number: {violation.ViolationNumber}").Bold();
            column.Item().Text($"Homeowner: {violation.HomeownerName}");
            column.Item().Text($"Type: {violation.ViolationType}");
            column.Item().Text($"Violation Date: {FormatDate(violation.ViolationDate)}");
            column.Item().Text($"Status: {violation.Status}");
            column.Item().Text($"Filed At: {FormatDate(violation.FiledAt)}");
            column.Item().Text($"Filed By: {DisplayValue(violation.FiledByUser?.Username)}");
            column.Item().Text($"Details: {DisplayValue(violation.Details)}");
            column.Item().Text($"Resolution: {DisplayValue(violation.Resolution)}");
        });
    }

    private static void ComposeTableReport(
        QuestPDF.Infrastructure.IContainer container,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        container.Column(column =>
        {
            column.Spacing(16);
            column.Item().PaddingTop(12).Text(DisplayValue(title)).Bold().FontSize(14);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var index = 0; index < headers.Count; index++)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var columnHeader in headers)
                    {
                        header.Cell().Element(cell => StyleHeaderCell(cell, columnHeader));
                    }
                });

                if (rows.Count == 0)
                {
                    table.Cell()
                        .ColumnSpan((uint)headers.Count)
                        .Border(1)
                        .BorderColor("#E2E8F0")
                        .Padding(8)
                        .Text("No records available.");

                    return;
                }

                foreach (var row in rows)
                {
                    for (var index = 0; index < headers.Count; index++)
                    {
                        var value = index < row.Count ? row[index] : "-";
                        table.Cell().Element(cell => StyleBodyCell(cell, DisplayValue(value)));
                    }
                }
            });
        });
    }

    private static void StyleHeaderCell(QuestPDF.Infrastructure.IContainer container, string text)
    {
        container
            .Background("#EEF2FF")
            .Border(1)
            .BorderColor("#E2E8F0")
            .Padding(6)
            .Text(text)
            .Bold()
            .FontColor("#1E293B");
    }

    private static void StyleBodyCell(QuestPDF.Infrastructure.IContainer container, string text)
    {
        container
            .Border(1)
            .BorderColor("#E2E8F0")
            .Padding(6)
            .Text(text);
    }

    private static string GetLocationLine(HOASettings? settings)
    {
        var parts = new[] { settings?.Subdivision, settings?.City, settings?.Province }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(", ", parts);
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var parts = new[] { homeowner.FirstName, homeowner.MiddleName, homeowner.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", parts);
    }

    private static string GetUnitDisplay(Homeowner homeowner)
    {
        if (!string.IsNullOrWhiteSpace(homeowner.Unit?.UnitNumber))
        {
            return homeowner.Unit.UnitNumber;
        }

        return "-";
    }

    private static string FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DateTime.TryParse(value, out var parsed))
        {
            return "-";
        }

        return parsed.ToString("MMMM dd, yyyy");
    }

    private static string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
        {
            return "-";
        }

        return System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static byte[]? TryReadLogo(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return null;
        }

        try
        {
            return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        }
        catch
        {
            return null;
        }
    }
}
