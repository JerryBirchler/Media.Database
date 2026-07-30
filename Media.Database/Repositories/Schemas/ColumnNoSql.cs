using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas
{
    public readonly struct ColumnsNoSql
    {
        public static readonly string Id = x();
        public static readonly string InsertedOn = x();
        public static readonly string IsCurrent = x();
        public static readonly string IsProperName = x();
        public static readonly string LastFileUpdate = x();
        public static readonly string Metadata = x();
        public static readonly string OriginalFilePath = x();
        public static readonly string SourceMachineId = x();
        public static readonly string UpdatedOn = x();
        public static readonly string Word = x();

        public static string x([CallerMemberName] string callerName = "")
        {
            var ordinal = Ordinals.GetField(callerName);
            return Ordinals.ToSnake(ordinal);
        }
    }
}
