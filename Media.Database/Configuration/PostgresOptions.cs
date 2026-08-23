namespace Media.Database.Configuration;

public class PostgresOptions
{
    public const string SectionName = "ConnectionStrings";

    public string PostgresConnection { get; set; } = string.Empty;
}
