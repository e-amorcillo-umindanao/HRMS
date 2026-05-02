using System.Globalization;

namespace HRMS.Helpers;

public static class FormatHelper
{
    public static string Peso(decimal amount) =>
        "\u20B1" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string Number(decimal amount) =>
        amount.ToString("N2", CultureInfo.InvariantCulture);
}
