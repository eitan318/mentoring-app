using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        // --- Helper: add parameters to command ---
        private static void AddParameters(SqliteCommand cmd, object parameters)
        {
            if (parameters == null)
                return;

            foreach (var prop in parameters.GetType().GetProperties())
            {
                cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
            }
        }

        // --- Helper: map a SqliteDataReader row to an object ---
        private static T MapReaderToObject<T>(SqliteDataReader reader) where T : new()
        {
            var result = new T();

            // Create HashSet manually (instead of ToHashSet)
            var columnNames = new HashSet<string>(
                Enumerable.Range(0, reader.FieldCount).Select(reader.GetName),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!columnNames.Contains(prop.Name))
                    continue;

                var value = reader[prop.Name];
                if (value == DBNull.Value)
                    continue;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(result, Convert.ChangeType(value, targetType));
            }

            return result;
        }

    }
}
