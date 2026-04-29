namespace HRMS.Helpers;

public static class AccessHelper
{
    private static readonly HashSet<string> SuperAdminWriteModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "auth",
        "msme",
        "settings",
        "subscriptions"
    };

    private static readonly HashSet<string> PresidentReadModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard", "homeowners", "units", "engagement", "events", "msme", "dues",
        "violations", "clearance", "documents", "reports"
    };

    private static readonly HashSet<string> PresidentWriteModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard", "homeowners", "units", "engagement", "events", "dues",
        "violations", "clearance", "clearance-approve", "documents", "violation-pdf", "reports"
    };

    private static readonly HashSet<string> BoardReadModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard", "homeowners", "units", "engagement", "events", "msme", "dues",
        "violations", "clearance", "documents", "reports"
    };

    private static readonly HashSet<string> StaffReadModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard", "homeowners", "units", "events", "dues", "clearance", "reports"
    };

    private static readonly HashSet<string> HomeownerReadModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "profile", "clearance"
    };

    public static bool CanWrite(string role, string module)
    {
        if (string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase))
        {
            return SuperAdminWriteModules.Contains(module);
        }

        if (string.Equals(role, "HOA President", StringComparison.OrdinalIgnoreCase))
        {
            return PresidentWriteModules.Contains(module);
        }

        if (string.Equals(role, "Board Member", StringComparison.OrdinalIgnoreCase))
        {
            return module.ToLowerInvariant() is not ("auth" or "clearance-approve" or "documents" or "msme" or "violation-pdf" or "reports" or "settings" or "subscriptions");
        }

        if (string.Equals(role, "HOA Staff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return module.ToLowerInvariant() is "homeowners" or "units" or "dues" or "events" or "clearance" or "reports";
        }

        return false;
    }

    public static bool CanRead(string role, string module)
    {
        if (string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(role, "HOA President", StringComparison.OrdinalIgnoreCase))
        {
            return PresidentReadModules.Contains(module);
        }

        if (string.Equals(role, "Board Member", StringComparison.OrdinalIgnoreCase))
        {
            return BoardReadModules.Contains(module);
        }

        if (string.Equals(role, "HOA Staff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return StaffReadModules.Contains(module);
        }

        if (string.Equals(role, "Homeowner", StringComparison.OrdinalIgnoreCase))
        {
            return HomeownerReadModules.Contains(module);
        }

        return false;
    }
}
