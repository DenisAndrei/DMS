using System.Data;

namespace DeviceManagement.Api.Infrastructure.Data;

public static class DataRecordExtensions
{
    public static int GetRequiredInt32(this IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.GetInt32(ordinal);
    }

    public static int? GetNullableInt32(this IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? null : record.GetInt32(ordinal);
    }

    public static string GetRequiredString(this IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.GetString(ordinal);
    }

    public static DateTime GetRequiredDateTime(this IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.GetDateTime(ordinal);
    }
}
