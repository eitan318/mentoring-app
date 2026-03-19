using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MentoringApp.Data.Acess.SQLite.ConnectionsService
{
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

        // --- Helper: map a SqliteDataReader row to an object ---
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

                // Handle Nullable types
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    // Special case for DateTime if stored as string in SQLite
                    if (targetType == typeof(DateTime) && value is string s)
                    {
                        prop.SetValue(result, DateTime.Parse(s));
                    }
                    // Special case for Enums
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
                    // Logic for logging mapping errors could go here
                }
            }

            return result;
        }
    }
}