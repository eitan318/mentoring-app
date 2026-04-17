using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlSettingsRepo : ISettingsRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlSettingsRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<double> GetDoubleAsync(string key, double defaultValue = 0)
        {
            var rows = await _db.QueryAsync<SettingRow>(
                "SELECT Value FROM Settings WHERE Key = @Key",
                new { Key = key });
            var row = rows.FirstOrDefault();
            if (row == null) return defaultValue;
            return double.TryParse(row.Value, out var v) ? v : defaultValue;
        }

        public async Task SetDoubleAsync(string key, double value)
        {
            await _db.ExecuteAsync(
                "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = @Value",
                new { Key = key, Value = value.ToString() });
        }

        public async Task<string> GetStringAsync(string key, string defaultValue = "")
        {
            var rows = await _db.QueryAsync<SettingRow>(
                "SELECT Value FROM Settings WHERE Key = @Key",
                new { Key = key });
            var row = rows.FirstOrDefault();
            if (row == null) return defaultValue;
            return row.Value ?? defaultValue;
        }

        public async Task SetStringAsync(string key, string value)
        {
            await _db.ExecuteAsync(
                "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = @Value",
                new { Key = key, Value = value });
        }

        private class SettingRow
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}
