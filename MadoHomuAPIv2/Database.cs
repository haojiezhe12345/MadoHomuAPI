using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    public static class Database
    {
        public static SqliteConnection OpenNewConnection()
        {
            return new SqliteConnection(@"Data Source=data\main.db");
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
                    property?.SetValue(dto, x.value != null
                        ? Convert.ChangeType(x.value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType)
                        : null);
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

        private class Row : List<RowValue>;

        private class RowValue
        {
            public required string column { get; set; }
            public object? value { get; set; }
        }
    }
}
