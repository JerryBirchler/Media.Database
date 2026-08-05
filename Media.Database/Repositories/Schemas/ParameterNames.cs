using System.Runtime.CompilerServices;

namespace Media.Database.Repositories.Schemas;

public readonly struct ParameterNames
{
    public static readonly string CameFromFileId = x();
    public static readonly string FileId = x();
    public static readonly string Id = x();
    public static readonly string InsertedOn = x();
    public static readonly string IsCurrent = x();
    public static readonly string IsProperName = x();
    public static readonly string LastFileUpdate = x();
    public static readonly string Limit = x();
    public static readonly string Metadata = x();
    public static readonly string Origin = x();
    public static readonly string OriginalFilePath = x();
    public static readonly string SourceMachineId = x();
    public static readonly string UpdatedOn = x();
    public static readonly string Word = x();
    public static readonly string WordId = x();
#pragma warning disable IDE1006 // Naming Styles
    public static string x([CallerMemberName] string callerName = "")
#pragma warning restore IDE1006 // Naming Styles
    {
        return $"@{Ordinals.GetField(callerName)}";
    }
}
