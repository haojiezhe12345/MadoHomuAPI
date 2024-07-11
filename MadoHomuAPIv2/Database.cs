using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    public static class Database
    {
        public static SqliteConnection OpenNewConnection()
        {
            var connection = new SqliteConnection(@"Data Source=data\main.db");
            connection.Open();
            return connection;
        }
    }

    public static class SqliteCommandExtensions
    {
        public static List<Dictionary<string, object?>> ReadAsDictList(this SqliteCommand command)
        {
            List<Dictionary<string, object?>> result = [];

            command.ReadEachRow(row =>
            {
                Dictionary<string, object?> dict = [];
                row.ForEach(x => dict[x.column] = x.value);
                result.Add(dict);
            });

            return result;
        }

        public static T? ReadAsDTO<T>(this SqliteCommand command) where T : new()
        {
            List<T> list = command.ReadAsDTOList<T>();
            return list.Count > 0 ? list[0] : default;
        }

        public static List<T> ReadAsDTOList<T>(this SqliteCommand command) where T : new()
        {
            List<T> result = [];
            var properties = typeof(T).GetProperties();

            command.ReadEachRow(row =>
            {
                T dto = new();
                row.ForEach(x =>
                {
                    var property = properties.FirstOrDefault(p => p.Name == x.column);
                    if (property != null && x.value != null)
                    {
                        try
                        {
                            property.SetValue(dto, Convert.ChangeType(x.value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
                        }
                        catch (Exception e)
                        {
                            Logging.Log($"Failed to convert database cell '{x.value}' of type {x.value.GetType()} to {property.PropertyType} at column '{x.column}'. Reason:\n{e}");
                        }
                    }
                });
                result.Add(dto);
            });

            return result;
        }

        private static void ReadEachRow(this SqliteCommand command, Action<Row> callback)
        {
            var reader = command.ExecuteReader();
            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            while (reader.Read())
            {
                Row row = [];
                columns.ForEach(column => row.Add(new RowValue
                {
                    column = column,
                    value = reader[column] != DBNull.Value ? reader[column] : null,
                }));
                callback(row);
            }
            reader.Close();
        }

        public static int InsertDTO<T>(this SqliteConnection connection, string table, T dto)
        {
            var properties = typeof(T).GetProperties();
            List<string> columns = [];
            List<string> values = [];

            var command = connection.CreateCommand();
            foreach (var property in properties)
            {
                columns.Add(property.Name);
                values.Add($"@{property.Name}");
                command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(dto) ?? DBNull.Value);
            }
            command.CommandText = $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

            return command.ExecuteNonQuery();
        }

        private class Row : List<RowValue>;

        private class RowValue
        {
            public required string column { get; set; }
            public object? value { get; set; }
        }
    }
}
