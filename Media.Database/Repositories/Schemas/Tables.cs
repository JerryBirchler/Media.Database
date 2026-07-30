using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Media.Database.Repositories.Schemas
{
    public readonly struct Tables
    {
        public static readonly string Files = n();
        public static readonly string Words = n();
        public static readonly string View_Current_Files = n();

#pragma warning disable IDE1006 // Naming Styles
        public static string n([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
        {
            return callerName;
        }

        public static string GetTable(string tableName)
        {
            FieldInfo field = typeof(Tables).GetField(tableName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new ArgumentException($"Table '{tableName}' was not found in Tables.");

            return (string)field.GetValue(null)!;
        }
        private static readonly Regex PascalToSnakeRegex = new(
            @"(?<!^)[A-Z]", RegexOptions.Compiled);

        public static string ToSnake(string ordinal)
        {
            FieldInfo field = typeof(Tables).GetField(ordinal, BindingFlags.Public | BindingFlags.Static)
                ?? throw new ArgumentException($"Table '{ordinal}' was not found in Tables.");

            if (string.IsNullOrWhiteSpace(ordinal)) return ordinal;
            return PascalToSnakeRegex.Replace(ordinal, m => $"_{m.Value.ToLower()}");
        }
    }
}
