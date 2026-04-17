using Microsoft.Data.Sqlite;

namespace MentoringApp.Data.Acess.SQLite.ConnectionsService
{
    public interface ISQLiteConnectionService
    {
        T QuerySingle<T>(string sql, object parameters = null) where T : new();
        List<T> Query<T>(string sql) where T : new();
        List<T> Query<T>(string sql, object parameters) where T : new();

        Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null) where T : new();

        Task<List<T>> QueryAsync<T>(string sql, object? parameters = null) where T : new();

        Task<int> ExecuteAsync(string sql, object? parameters = null);

        int Execute(string sql, object parameters = null);

    }
}
