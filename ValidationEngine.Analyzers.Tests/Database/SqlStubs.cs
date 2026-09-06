namespace ValidationEngine.Analyzers.Database.Tests;

internal static class SqlStubs
{
    public const string SqlCommandStub = @"
namespace System.Data
{
    public enum CommandType
    {
        Text,
        StoredProcedure
    }
}

namespace System.Data.SqlClient
{
    public class SqlParameterCollection
    {
        public void AddWithValue(string parameterName, object value) { }
    }

    public class SqlCommand
    {
        public SqlCommand() { }
        public SqlCommand(string commandText) { }
        public SqlCommand(string commandText, SqlConnection connection) { }

        public string CommandText { get; set; }
        public System.Data.CommandType CommandType { get; set; }
        public SqlParameterCollection Parameters { get; } = new SqlParameterCollection();
    }

    public class SqlConnection
    {
        public SqlConnection(string connectionString) { }
    }
}
";

    public const string SqlDataReaderStub = @"
namespace System.Data.SqlClient
{
    public class SqlDataReader
    {
        public object this[int index] => null;
        public object this[string name] => null;

        public int GetOrdinal(string name) => 0;
        public int GetInt32(int ordinal) => 0;
        public string GetString(int ordinal) => null;
    }
}
";
}
