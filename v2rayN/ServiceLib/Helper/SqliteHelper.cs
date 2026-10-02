using System.Collections;

namespace ServiceLib.Helper;

public sealed class SQLiteHelper
{
    private static readonly Lazy<SQLiteHelper> _instance = new(() => new());
    public static SQLiteHelper Instance => _instance.Value;
    private readonly string _connstr;
    private SQLiteConnection _db;
    private SQLiteAsyncConnection _dbAsync;
    private readonly string _configDB = "guiNDB.db";
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public string DatabasePath => _connstr;

    public SQLiteHelper()
    {
        _connstr = Utils.GetConfigPath(_configDB);
        _db = new SQLiteConnection(_connstr, false);
        _dbAsync = new SQLiteAsyncConnection(_connstr, false);
    }

    public CreateTableResult CreateTable<T>()
    {
        _writeGate.Wait();
        try
        {
            return _db.CreateTable<T>();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public Task<int> InsertAllAsync(IEnumerable models)
        => WithWriteGateAsync(() => _dbAsync.InsertAllAsync(models, runInTransaction: true));

    public Task<int> InsertAsync(object model)
        => WithWriteGateAsync(() => _dbAsync.InsertAsync(model));

    public Task<int> ReplaceAsync(object model)
        => WithWriteGateAsync(() => _dbAsync.InsertOrReplaceAsync(model));

    public Task<int> UpdateAsync(object model)
        => WithWriteGateAsync(() => _dbAsync.UpdateAsync(model));

    public Task<int> UpdateAllAsync(IEnumerable models)
        => WithWriteGateAsync(() => _dbAsync.UpdateAllAsync(models, runInTransaction: true));

    public Task<int> DeleteAsync(object model)
        => WithWriteGateAsync(() => _dbAsync.DeleteAsync(model));

    public Task<int> DeleteAllAsync<T>()
        => WithWriteGateAsync(() => _dbAsync.DeleteAllAsync<T>());

    public Task<int> ExecuteAsync(string sql, params object[] args)
        => WithWriteGateAsync(() => _dbAsync.ExecuteAsync(sql, args));

    public void RunInTransaction(Action<SQLiteConnection> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _writeGate.Wait();
        try
        {
            _db.RunInTransaction(() => action(_db));
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public Task RunInTransactionAsync(Action<SQLiteConnection> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return WithWriteGateAsync(() => _dbAsync.RunInTransactionAsync(action));
    }

    /// <summary>
    /// Serializes a maintenance operation against every application write routed through this helper.
    /// The callback must use the supplied synchronous connection for write operations so it does not
    /// recursively acquire the write gate.
    /// </summary>
    public async Task RunExclusiveWriteAsync(
        Func<SQLiteConnection, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action(_db).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
    {
        return await _dbAsync.QueryAsync<T>(sql, args);
    }

    public AsyncTableQuery<T> TableAsync<T>() where T : new()
    {
        return _dbAsync.Table<T>();
    }

    public async Task DisposeDbConnectionAsync()
    {
        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
            {
                try
                {
                    _db?.Close();
                    _db?.Dispose();
                }
                finally
                {
                    _db = null;
                }

                try
                {
                    var conn = _dbAsync?.GetConnection();
                    conn?.Close();
                    conn?.Dispose();
                }
                finally
                {
                    _dbAsync = null;
                }
            });
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task<T> WithWriteGateAsync<T>(Func<Task<T>> action)
    {
        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task WithWriteGateAsync(Func<Task> action)
    {
        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }
}
