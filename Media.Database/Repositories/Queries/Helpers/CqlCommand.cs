using Cassandra;
using System.Text.RegularExpressions;

#pragma warning disable CS8981 
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981 

namespace Media.Database.Repositories.Queries.Helpers;

/// <summary>
/// Wraps a parameterized CQL query, translating <c>@name</c> placeholders to positional
/// bind parameters and supporting both single-statement execution and id-keyed batching.
/// </summary>
/// <param name="session">The Cassandra/Scylla session to execute against.</param>
/// <param name="parameterizedQuery">The CQL query text, with named (<c>@name</c>) parameters.</param>
/// <param name="batchSize">The number of statements to accumulate before flushing a batch.</param>
public class CqlCommand(ISession session, string parameterizedQuery, int batchSize = 100)
{
    private static readonly Regex ParamRegex = new(@"@([a-zA-Z0-9_]+)", RegexOptions.Compiled);
    private readonly ISession _session = session;

    private readonly List<string>? _parameterList = [.. ParamRegex.Matches(parameterizedQuery)
                       .Cast<Match>()
                       .Select(m => m.Groups[1].Value)];

    private readonly string _cqlNativeQuery = ParamRegex.Replace(parameterizedQuery, "?");
    private readonly int _batchSize = batchSize;
    private BatchStatement _batch = null!;
    private int _rows = 0;

    /// <summary>
    /// Gets or sets the parameter values to bind, keyed by upper-invariant <c>@NAME</c>.
    /// </summary>
    public SortedDictionary<string, object> Parameters { get; set; } = [];

    /// <summary>
    /// Binds <see cref="Parameters"/> and executes the query as a single statement.
    /// </summary>
    /// <returns>The resulting row set.</returns>
    public async Task<RowSet> ExecuteRowSet()
    {
        var boundStatement = Bind();
        return await _session.ExecuteAsync(boundStatement);
    }

    /// <summary>
    /// Prepares the query and binds the current <see cref="Parameters"/> values, in the order they appear in the query text.
    /// </summary>
    /// <returns>The bound statement.</returns>
    /// <exception cref="ArgumentException">Thrown when a placeholder in the query has no matching entry in <see cref="Parameters"/>.</exception>
    public BoundStatement Bind()
    {
        var ps = _session.Prepare(_cqlNativeQuery);
        List<object> values = [];
        foreach (var param in _parameterList ?? [])
        {
            if (Parameters.TryGetValue("@" + param.ToUpperInvariant(), out var value))
                values.Add(value);

            else
                throw new ArgumentException($"Parameter '{param}' is missing in the Parameters dictionary.");
        }

        return ps.Bind([.. values]);
    }

    /// <summary>
    /// Executes a previously assembled batch statement.
    /// </summary>
    /// <param name="batch">The batch statement to execute.</param>
    public async Task ExecuteAsync(BatchStatement batch)
    {
        await _session.ExecuteAsync(batch);
    }

    /// <summary>
    /// Starts a new batch, resetting the row counter.
    /// </summary>
    public void BeginBatch()
    {
        _batch = new BatchStatement();
        _rows = 0;
    }

    /// <summary>
    /// Binds the query with the given id as its sole parameter and adds it to the current batch,
    /// flushing and starting a new batch automatically once <paramref name="id"/> fills it to <c>batchSize</c>.
    /// </summary>
    /// <param name="id">The id to bind for this statement.</param>
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

    /// <summary>
    /// Flushes any statements remaining in the current batch and clears it.
    /// </summary>
    public async Task EndBatch()
    {
        if (_rows % _batchSize != 0)
            await this.ExecuteAsync(_batch);

        _batch = null!;
    }
}
