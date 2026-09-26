using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;
using System.Text;
#pragma warning disable CS8981
using cw = Media.Database.Repositories.Schemas.TablesSql.View_WordFilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// Builds the SQL that executes a combined search: lines OR together within a list, lists AND
/// with one another, and file criteria narrow the whole result.
///
/// The shape is relational division. A file matches when, among the word rows belonging to it, at
/// least one satisfies every list -- which is <c>GROUP BY "FileId"</c> with a <c>bool_or</c> per
/// list in the <c>HAVING</c>. Expressing it as one predicate per list keeps the AND legible and
/// means adding a list costs one more term rather than a restructured query.
///
/// The leading <c>Word = ANY(...)</c> is not redundant with the HAVING. Without it the group-by
/// reads the whole view; with it, every candidate row is reached through IX_Words_Word and the
/// grouping runs over a handful of rows. The words of every list are unioned into that one array,
/// because a file can only satisfy the AND if it matches at least one word from each -- so no
/// file is excluded by pre-filtering on all of them.
///
/// Every parameter carries an explicit cast. A null passed as DBNull gives Npgsql nothing to infer
/// a type from, and Postgres then refuses the statement outright rather than treating it as "any"
/// -- so the optional-filter pattern only works here if the type is stated.
///
/// Parameter names are generated rather than drawn from the ParameterNames registry, which cannot
/// express a variable-length set. They are never string-interpolated from user input: only the
/// generated name reaches the SQL text, and every value is bound.
/// </summary>
public static class QueryFileSearch
{
    /// <summary>
    /// Builds the statement and the parameter values for it.
    /// </summary>
    /// <param name="lists">
    /// The OR lists to AND together. An empty list is vacuously true and is dropped rather than
    /// matching nothing -- clearing a filter must not silently return zero rows.
    /// </param>
    /// <param name="isCurrent">Restrict to current files, deleted files, or null for both.</param>
    /// <param name="sourceMachineIds">Restrict to files from these devices, or empty for any.</param>
    /// <param name="limit">A cap, so a broad search cannot return the entire corpus.</param>
    public static (string Sql, IReadOnlyDictionary<string, object> Parameters) Build(
        IReadOnlyList<IReadOnlyList<SearchListLine>> lists,
        bool? isCurrent,
        IReadOnlyList<int> sourceMachineIds,
        int limit)
    {
        var parameters = new Dictionary<string, object>();
        var words = new List<string>();
        var listPredicates = new List<string>();

        var populated = lists.Where(l => l.Count > 0).ToList();

        for (var listIndex = 0; listIndex < populated.Count; listIndex++)
        {
            var linePredicates = new List<string>();

            for (var lineIndex = 0; lineIndex < populated[listIndex].Count; lineIndex++)
            {
                var line = populated[listIndex][lineIndex];
                var key = $"{listIndex}_{lineIndex}";

                var wordParameter = $"@w{key}";
                parameters[wordParameter] = line.LookingFor;
                words.Add(line.LookingFor);

                var predicate = new StringBuilder($"{cw.Word} = {wordParameter}");

                // Absent means any. Expressed as a parameter rather than by omitting the term so
                // that the statement's text is the same shape for every line, which keeps it
                // reusable in Postgres's plan cache.
                var originParameter = $"@o{key}";
                parameters[originParameter] = line.MetadataType is null ? DBNull.Value : (int)line.MetadataType;
                predicate.Append($" AND ({originParameter}::int IS NULL OR {cw.Origin} = {originParameter}::int)");

                var typeParameter = $"@t{key}";
                parameters[typeParameter] = line.WordType is null ? DBNull.Value : (int)line.WordType;
                predicate.Append($" AND ({typeParameter}::int IS NULL OR {cw.WordType} = {typeParameter}::int)");

                linePredicates.Add($"({predicate})");
            }

            listPredicates.Add($"bool_or({string.Join(" OR ", linePredicates)})");
        }

        var sql = new StringBuilder();

        sql.AppendLine($"SELECT {cw.FileId}, {cw.OriginalFilePath}, {cw.IsCurrent}, {cw.SourceMachineId}");
        sql.AppendLine($"FROM {ts.View_WordFiles}");
        sql.AppendLine("WHERE 1 = 1");

        if (words.Count > 0)
        {
            parameters["@words"] = words.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            sql.AppendLine($"  AND {cw.Word} = ANY(@words::text[])");
        }

        // The same optional-filter pattern the word queries already use: null means any.
        parameters["@isCurrent"] = isCurrent is null ? DBNull.Value : isCurrent.Value;
        sql.AppendLine($"  AND (@isCurrent::boolean IS NULL OR {cw.IsCurrent} = @isCurrent::boolean)");

        parameters["@sourceMachineIds"] = sourceMachineIds.Count > 0 ? sourceMachineIds.ToArray() : DBNull.Value;
        sql.AppendLine($"  AND (@sourceMachineIds::int[] IS NULL OR {cw.SourceMachineId} = ANY(@sourceMachineIds::int[]))");

        sql.AppendLine($"GROUP BY {cw.FileId}, {cw.OriginalFilePath}, {cw.IsCurrent}, {cw.SourceMachineId}");

        if (listPredicates.Count > 0)
            sql.AppendLine($"HAVING {string.Join(" AND ", listPredicates)}");

        // Ordered by path, matching how files already page, so results are stable and a cursor
        // can be layered on later without changing what a page contains.
        sql.AppendLine($"ORDER BY {cw.OriginalFilePath}");

        parameters["@limit"] = limit;
        sql.AppendLine("LIMIT @limit;");

        return (sql.ToString(), parameters);
    }
}

/// <summary>Row mapping for <see cref="QueryFileSearch"/>.</summary>
public static class FileSearchMappers
{
    /// <summary>Maps the current row to a <see cref="FileSearchResult"/>.</summary>
    public static FileSearchResult ToFileSearchResult(this NpgsqlDataReader reader)
    {
        return new FileSearchResult
        {
            FileId = reader.GetGuid(os.FileId),
            OriginalFilePath = reader.GetString(os.OriginalFilePath),
            IsCurrent = reader.GetFieldValue<bool>(os.IsCurrent),
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
        };
    }
}
