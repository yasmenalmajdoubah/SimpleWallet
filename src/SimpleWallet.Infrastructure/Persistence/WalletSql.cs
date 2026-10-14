using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace SimpleWallet.Infrastructure.Persistence;

internal static class WalletSql
{
    public static bool IsSqlite(string connectionString) =>
        connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

    public static DbConnection Open(string connectionString) =>
        IsSqlite(connectionString)
            ? new SqliteConnection(connectionString)
            : new SqlConnection(connectionString);

    public static object FormatId(Guid id) => id.ToString("D").ToUpperInvariant();

    static WalletSql()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.AddTypeHandler(new GuidHandler());
    }

    private sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value;

        public override Guid Parse(object value) => value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => Guid.Parse(Convert.ToString(value) ?? "")
        };
    }
}
