using AvailabilityService.Infrastructure;
using System.Data;
using System.Data.Common;
using Xunit;

namespace AvailabilityService.Tests.Infrastructure;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ExecutesCreateTablesScript()
    {
        var connection = new RecordingDbConnection();
        var initializer = new DatabaseInitializer(() => connection);

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(1, connection.ExecuteNonQueryCalls);
        Assert.Contains("CREATE TABLE dbo.AvailabilityRules", connection.LastCommandText, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE dbo.AvailabilityExceptions", connection.LastCommandText, StringComparison.Ordinal);
    }

    private sealed class RecordingDbConnection : DbConnection
    {
        public int ExecuteNonQueryCalls { get; private set; }
        public string LastCommandText { get; private set; } = string.Empty;

        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "1.0";
        private ConnectionState _state = ConnectionState.Closed;
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        public override Task OpenAsync(CancellationToken cancellationToken) { _state = ConnectionState.Open; return Task.CompletedTask; }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new RecordingDbCommand(this);

        internal int RecordExecution(string commandText)
        {
            ExecuteNonQueryCalls++;
            LastCommandText = commandText;
            return 1;
        }
    }

    private sealed class RecordingDbCommand : DbCommand
    {
        private readonly RecordingDbConnection _connection;

        public RecordingDbCommand(RecordingDbConnection connection) => _connection = connection;

        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get => _connection; set { } }
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new RecordingParameterCollection();

        public override void Cancel() { }
        public override int ExecuteNonQuery() => _connection.RecordExecution(CommandText);
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new RecordingParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(_connection.RecordExecution(CommandText));
    }

    private sealed class RecordingParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();

        public override int Count => _items.Count;
        public override object SyncRoot { get; } = new();
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var item in values) Add(item!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => _items.Any(x => x.ParameterName == value);
        public override void CopyTo(Array array, int index) => throw new NotSupportedException();
        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(x => x.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAll(x => x.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items.First(x => x.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0) _items[index] = value;
            else _items.Add(value);
        }
    }

    private sealed class RecordingParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }
}
