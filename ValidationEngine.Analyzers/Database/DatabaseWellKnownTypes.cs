namespace ValidationEngine.Analyzers.Database;

internal static class DatabaseWellKnownTypes
{
    public const string SqlCommandMetadataName = "System.Data.SqlClient.SqlCommand";
    public const string MicrosoftSqlCommandMetadataName = "Microsoft.Data.SqlClient.SqlCommand";
    public const string CommandTypePropertyName = "CommandType";
    public const string CommandTypeEnumMetadataName = "System.Data.CommandType";
    public const string CommandTypeStoredProcedureMemberName = "StoredProcedure";
    public const string CommandTypeTextMemberName = "Text";

    public const string SqlParameterCollectionMetadataName = "System.Data.SqlClient.SqlParameterCollection";
    public const string MicrosoftSqlParameterCollectionMetadataName = "Microsoft.Data.SqlClient.SqlParameterCollection";
    public const string AddWithValueMethodName = "AddWithValue";
    public const string DbNullMetadataName = "System.DBNull";
    public const string DbNullValuePropertyName = "Value";

    public const string SqlDataReaderMetadataName = "System.Data.SqlClient.SqlDataReader";
    public const string MicrosoftSqlDataReaderMetadataName = "Microsoft.Data.SqlClient.SqlDataReader";
    public const string DbDataReaderMetadataName = "System.Data.Common.DbDataReader";

    public const string ConnectionStringPoolingSegment = "Pooling=false";
}
