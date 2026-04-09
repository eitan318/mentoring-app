using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MentoringApp.Data.Acess.SQLite.ConnectionsService
{
    /// <summary>
    /// Lightweight ADO.NET wrapper over SQLite.
    /// Opens a new connection per call (no connection pooling concern — SQLite handles this).
    /// Provides synchronous and async variants for single-row queries, multi-row queries,
    /// and non-query execution.
    /// Results are mapped to typed objects via reflection in <see cref="MapReaderToObject{T}"/>.
    /// </summary>
    public class SQLiteConnectionService : ISQLiteConnectionService
    {
        private readonly string _connectionString;

        public SQLiteConnectionService(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
        }
        // --- Single row query ---
        public T QuerySingle<T>(string sql, object parameters = null) where T : new()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return default!;

            return MapReaderToObject<T>(reader);
        }

        // --- Multiple row query (without parameters) ---
        public List<T> Query<T>(string sql) where T : new()
        {
            return Query<T>(sql, null);
        }

        // --- Multiple row query (with parameters) ---
        public List<T> Query<T>(string sql, object parameters) where T : new()
        {
            var list = new List<T>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapReaderToObject<T>(reader));
            }

            return list;
        }

        // --- Execute non-query (INSERT, UPDATE, DELETE) ---
        public int Execute(string sql, object parameters = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            return cmd.ExecuteNonQuery();
        }
        // --- Async Single row query ---
        public async Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null) where T : new()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return default;

            return MapReaderToObject<T>(reader);
        }

        // --- Async Multiple row query ---
        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null) where T : new()
        {
            var list = new List<T>();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToObject<T>(reader));
            }

            return list;
        }

        // --- Async Execute non-query ---
        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand(sql, conn);
            AddParameters(cmd, parameters);

            return await cmd.ExecuteNonQueryAsync();
        }

        // --- Helper: add parameters to command ---
        private static void AddParameters(SqliteCommand cmd, object? parameters)
        {
            if (parameters == null) return;

            foreach (var prop in parameters.GetType().GetProperties())
            {
                var value = prop.GetValue(parameters) ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@" + prop.Name, value);
            }
        }

        /// <summary>
        /// Maps a single reader row to a new instance of <typeparamref name="T"/> using reflection.
        /// Matching is case-insensitive by column name. Columns not present as writable properties
        /// (and vice versa) are silently skipped, making the mapper tolerant of schema changes.
        /// Special handling: SQLite stores DateTime as ISO-8601 string and booleans as integers.
        /// </summary>
        private static T MapReaderToObject<T>(SqliteDataReader reader) where T : new()
        {
            var result = new T();
            var fieldCount = reader.FieldCount;

            // Build a set of column names present in the reader for quick lookup
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite || !columnNames.Contains(prop.Name))
                    continue;

                var value = reader[prop.Name];
                if (value == DBNull.Value)
                    continue;

                // Unwrap Nullable<T> so Convert.ChangeType receives the underlying type
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    // SQLite stores DateTime as an ISO-8601 string
                    if (targetType == typeof(DateTime) && value is string s)
                    {
                        prop.SetValue(result, DateTime.Parse(s));
                    }
                    // SQLite has no native enum type; values are stored as integers
                    else if (targetType.IsEnum)
                    {
                        prop.SetValue(result, Enum.ToObject(targetType, value));
                    }
                    else
                    {
                        prop.SetValue(result, Convert.ChangeType(value, targetType));
                    }
                }
                catch
                {
                    // Silently skip columns that cannot be converted; avoids crashing on schema mismatches
                }
            }

            return result;
        }
    }
}