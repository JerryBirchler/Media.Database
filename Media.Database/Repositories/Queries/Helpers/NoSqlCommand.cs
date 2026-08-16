using Cassandra;
using System.Text.RegularExpressions;

#pragma warning disable CS8981 
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries.Helpers;

internal class NoSqlCommand(ISession session, string parameterizedQuery, int batchSize = 100)
{
    private static readonly Regex ParamRegex = new(@"@([a-zA-Z0-9_]+)", RegexOptions.Compiled);
    private readonly ISession _session = session;

    private readonly List<string>? _parameterList = [.. ParamRegex.Matches(parameterizedQuery)
                       .Cast<Match>()
                       .Select(m => m.Groups[1].Value)];

    private readonly string _noSqlNativeQuery = ParamRegex.Replace(parameterizedQuery, "?");
    private readonly int _batchSize = batchSize;
    private BatchStatement _batch = null!;
    private int _rows = 0;

    public SortedDictionary<string, object> Parameters { get; set; } = [];

    public async Task<RowSet> ExecuteRowSet()
    {
        var boundStatement = Bind();
        return await _session.ExecuteAsync(boundStatement);
    }

    public BoundStatement Bind()
    {
        var ps = _session.Prepare(_noSqlNativeQuery);
        List<object> values = [];
        foreach (var param in _parameterList ?? [])
        {
            if (Parameters.TryGetValue("@" + param.ToUpperInvariant(), out var value))
                values.Add(value);

            else
                throw new ArgumentException($"Parameter '{param}' is missing in the Parameters dictionary.");
        }

        return ps.Bind(values.ToArray());
    }

    public async Task ExecuteAsync(Cassandra.BatchStatement batch)
    {
        await _session.ExecuteAsync(batch);
    }

    public void BeginBatch()
    {
        _batch = new BatchStatement();
        _rows = 0;
    }

    public async Task AddQuery(Guid id)
    {
        _rows++;
        this.Parameters.Clear();
        this.Parameters.AddWithValue(pn.Id, id);
        _batch.Add(this.Bind());

        if (_rows % _batchSize == 0)
        {
            await this.ExecuteAsync(_batch);
            _batch = new BatchStatement();
        }
    }

    public async Task EndBatch()
    {
        if (_rows % _batchSize != 0)
            await this.ExecuteAsync(_batch);

        _batch = null!;
    }
}
