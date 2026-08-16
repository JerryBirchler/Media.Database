namespace Media.Database.Repositories.Schemas;

internal class Tables : BaseSchema<Tables, NoSubFields>
{
    public static readonly string Files = x();
    public static readonly string WordFiles = x();
    public static readonly string Words = x();
    public static readonly string View_Current_Files = x();
    public static readonly string View_WordFiles = x();
}
