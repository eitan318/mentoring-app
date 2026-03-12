namespace MentoringApp.Data.Acess.SQLite.ConnectionsService
{
    public interface ISQLiteConnectionService
    {
        /// <summary>
        /// Executes a SQL query that returns a single row and maps it to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type to map the row to.</typeparam>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <returns>An instance of T with the row data, or default if no row is found.</returns>
        T QuerySingle<T>(string sql, object parameters = null) where T : new();

        /// <summary>
        /// Executes a SQL query that returns multiple rows and maps them to a list of type T.
        /// </summary>
        /// <typeparam name="T">The type to map each row to.</typeparam>
        /// <param name="sql">The SQL query.</param>
        /// <returns>A list of T containing the query results.</returns>
        List<T> Query<T>(string sql) where T : new();
        List<T> Query<T>(string sql, object parameters) where T : new();

        int Execute(string sql, object parameters = null);

    }
}
