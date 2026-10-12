namespace RestaurantManagement.Web.Services;

public static class CsvExport
{
    public static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var trimmed = value.TrimStart();
        // Names entered by other roles must not become spreadsheet formulas.
        if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) || value.StartsWith('\t') || value.StartsWith('\r')) value = "'" + value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
