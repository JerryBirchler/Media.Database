using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Media.Database.Repositories.Schemas
{
    public readonly struct Ordinals
    {
        public static readonly string Id = n();
        public static readonly string InsertedOn = n();
        public static readonly string IsCurrent = n();
        public static readonly string IsProperName = n();
        public static readonly string LastFileUpdate = n();
        public static readonly string Limit = n();
        public static readonly string Metadata = n();
        public static readonly string OriginalFilePath = n();
        public static readonly string SourceMachineId = n();
        public static readonly string UpdatedOn = n();
        public static readonly string Word = n();

#pragma warning disable IDE1006 // Naming Styles
        public static string n([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
        {
            return callerName;
        }

        public static string GetField(string fieldName)
        {
            FieldInfo field = typeof(Ordinals).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new ArgumentException($"Ordinal '{fieldName}' was not found in {nameof(Ordinals)}.");

            return (string)field.GetValue(null)!;
        }

        private static readonly Regex PascalToSnakeRegex = new(
            @"(?<!^)[A-Z]", RegexOptions.Compiled);

        public static string ToSnake(string ordinal)
        {
            if (string.IsNullOrWhiteSpace(ordinal)) 
                return ordinal;

            return PascalToSnakeRegex.Replace(ordinal, m => $"_{m.Value.ToLower()}");
        }

    }
}
