using System.Data;
using System.Data.SQLite;
using Xunit;

namespace DatabaseManager.Tests;

/// <summary>Checks ownership and failure recovery of SQLite manager connections.</summary>
public class SQLiteManagerLifetimeTests
{
    /// <summary>Checks successful helpers preserve the caller's connection ownership.</summary>
    /// <param name="helper">The helper to invoke.</param>
    /// <param name="initiallyOpen">Whether the caller owns an open connection.</param>
    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)]
    public void HelpersPreserveInitialState(int helper, bool initiallyOpen)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path);
            if (initiallyOpen) manager.Open();
            Invoke(manager, helper);
            Assert.Equal(initiallyOpen, manager.DataBaseOpen);
            Assert.Equal(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, manager.DbConnection.State);
            if (initiallyOpen) Assert.Equal(1L, Scalar(manager, "SELECT 1"));
            else AssertReleased(path);
        });
    }

    /// <summary>Checks partial acquisition failures close owned connections and preserve the original error.</summary>
    /// <param name="helper">The helper to invoke.</param>
    /// <param name="cleanupFails">Whether close also reports an error.</param>
    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)]
    public void PartialOpenFailureIsCleaned(int helper, bool cleanupFails)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path) { FailOpen = true, FailClose = cleanupFails };
            var error = Assert.Throws<InvalidOperationException>(() => Invoke(manager, helper));
            Assert.Same(manager.OpenError, error);
            Assert.Contains(nameof(ProbeManager.Open), error.StackTrace);
            Assert.Equal(1, manager.CloseCalls);
            Assert.False(manager.DataBaseOpen);
            Assert.Equal(ConnectionState.Closed, manager.DbConnection.State);
            AssertReleased(path);
            manager.FailOpen = manager.FailClose = false;
            Invoke(manager, helper);
        });
    }

    /// <summary>Checks cleanup-only failure propagates after successful work.</summary>
    /// <param name="helper">The helper to invoke.</param>
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void CleanupOnlyFailurePropagates(int helper)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path) { FailClose = true };
            Assert.Same(manager.CloseError, Assert.Throws<InvalidOperationException>(() => Invoke(manager, helper)));
            AssertReleased(path);
        });
    }

    /// <summary>Checks vacuum failure leaves the caller's active transaction usable.</summary>
    [Fact]
    public void VacuumDoesNotCloseCallerTransaction()
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path);
            manager.Open();
            using var transaction = manager.DbConnection.BeginTransaction();
            Assert.Throws<SQLiteException>(() => manager.Vacuum());
            Assert.True(manager.DataBaseOpen);
            Assert.Equal(0, manager.CloseCalls);
            using var command = new SQLiteCommand("CREATE TABLE retained (value INTEGER)", manager.DbConnection, transaction);
            command.ExecuteNonQuery();
            transaction.Commit();
            Assert.Contains("retained", manager.GetTableNames());
        });
    }

    /// <summary>Checks both constructors release provider resources when initialization throws.</summary>
    /// <param name="passwordOverload">Whether to use the password constructor.</param>
    /// <param name="cleanupFails">Whether provider disposal reports a secondary error.</param>
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void ConstructorFailureDisposesConnectionDirectly(bool passwordOverload, bool cleanupFails)
    {
        WithFile(path =>
        {
            SQLiteConnection? captured = null;
            var primary = new InvalidOperationException("initialization failure");
            var cleanupObserved = false;
            EventHandler cleanupFailure = (_, _) => { cleanupObserved = true; throw new IOException("provider cleanup failure"); };
            Action<SQLiteConnection> fail = connection =>
            {
                captured = connection;
                connection.Open();
                if (cleanupFails) connection.Disposed += cleanupFailure;
                throw primary;
            };
            var error = Assert.Throws<InvalidOperationException>(() =>
            {
                if (passwordOverload) _ = new FailingConstructorManager(path, null!, fail);
                else _ = new FailingConstructorManager(path, fail);
            });
            Assert.Same(primary, error);
            Assert.Contains(nameof(FailingConstructorManager.GetTableNames), error.StackTrace);
            Assert.NotNull(captured);
            try
            {
                if (cleanupFails)
                {
                    Assert.True(cleanupObserved);
                    Assert.Equal(ConnectionState.Closed, captured.State);
                }
                else
                {
                    Assert.Throws<ObjectDisposedException>(() => captured.State);
                    Assert.Throws<ObjectDisposedException>(() => captured.Open());
                }
                AssertReleased(path);
            }
            finally { captured.Disposed -= cleanupFailure; captured.Dispose(); }
        });
    }

    /// <summary>Checks both constructors successfully initialize and leave the file released.</summary>
    /// <param name="passwordOverload">Whether to use the password constructor.</param>
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ConstructorsLeaveConnectionClosed(bool passwordOverload)
    {
        WithFile(path =>
        {
            using var manager = passwordOverload ? new SQLiteManager(path, null!, Builder()) : new SQLiteManager(path, Builder());
            Assert.False(manager.DataBaseOpen);
            Assert.Equal(ConnectionState.Closed, manager.DbConnection.State);
            AssertReleased(path);
            manager.Open();
            using (var command = new SQLiteCommand("CREATE TABLE successful (value INTEGER); INSERT INTO successful VALUES (7);", manager.DbConnection))
                command.ExecuteNonQuery();
            Assert.Equal(7L, Scalar(manager, "SELECT value FROM successful"));
            manager.Close();
            Assert.Contains("successful", manager.GetTableNames());
        });
    }

    /// <summary>Checks real invalid-file failures release owned acquisitions and recover after repair.</summary>
    /// <param name="helper">The helper to invoke.</param>
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void InvalidDatabaseFailureRecovers(int helper)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path);
            File.WriteAllText(path, "This is not a SQLite database.");
            Assert.Throws<SQLiteException>(() => Invoke(manager, helper));
            Assert.False(manager.DataBaseOpen);
            Assert.Equal(ConnectionState.Closed, manager.DbConnection.State);
            Assert.Equal(1, manager.CloseCalls);
            AssertReleased(path);
            File.Delete(path);
            SQLiteManager.CreateSqLiteFile(path);
            Invoke(manager, helper);
            Assert.False(manager.DataBaseOpen);
        });
    }

    /// <summary>Checks a real exclusive lock failure preserves ownership and recovers once released.</summary>
    /// <param name="initiallyOpen">Whether the manager's connection is caller-owned.</param>
    [Theory] [InlineData(false)] [InlineData(true)]
    public void LockedVacuumRecovers(bool initiallyOpen)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path);
            if (initiallyOpen) manager.Open();
            var builder = Builder();
            builder.DataSource = path;
            using (var blocker = new SQLiteConnection(builder.ConnectionString))
            {
                blocker.Open();
                using var begin = new SQLiteCommand("BEGIN EXCLUSIVE", blocker);
                begin.ExecuteNonQuery();
                try
                {
                    Assert.Throws<SQLiteException>(() => manager.Vacuum());
                    Assert.Equal(initiallyOpen, manager.DataBaseOpen);
                    Assert.Equal(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, manager.DbConnection.State);
                    if (!initiallyOpen) Assert.Equal(1, manager.CloseCalls);
                }
                finally
                {
                    using var rollback = new SQLiteCommand("ROLLBACK", blocker);
                    rollback.ExecuteNonQuery();
                }
            }
            manager.Vacuum();
            Assert.Equal(initiallyOpen, manager.DataBaseOpen);
            if (initiallyOpen) Assert.Equal(1L, Scalar(manager, "SELECT 1"));
            else AssertReleased(path);
        });
    }

    /// <summary>Checks SQL failures restore owned connections and preserve borrowed connections.</summary>
    /// <param name="helper">The helper to invoke.</param>
    /// <param name="initiallyOpen">Whether the connection is caller-owned.</param>
    /// <param name="cleanupFails">Whether owned close reports a secondary error.</param>
    [Theory]
    [InlineData(0, false, false)] [InlineData(1, false, false)] [InlineData(2, false, false)]
    [InlineData(0, true, false)] [InlineData(1, true, false)] [InlineData(2, true, false)]
    [InlineData(0, false, true)] [InlineData(1, false, true)] [InlineData(2, false, true)]
    public void SqlFailurePreservesOwnershipAndRecovers(int helper, bool initiallyOpen, bool cleanupFails)
    {
        WithFile(path =>
        {
            using var manager = new ProbeManager(path);
            manager.DbConnection.Authorize += (_, args) =>
            {
                if (manager.RejectSql) args.ReturnCode = SQLiteAuthorizerReturnCode.Deny;
            };
            if (initiallyOpen) manager.Open();
            manager.RejectSql = true;
            manager.FailClose = cleanupFails;
            var error = Assert.Throws<SQLiteException>(() => Invoke(manager, helper));
            Assert.Contains("authoriz", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(initiallyOpen, manager.DataBaseOpen);
            Assert.Equal(initiallyOpen ? ConnectionState.Open : ConnectionState.Closed, manager.DbConnection.State);
            Assert.Equal(initiallyOpen ? 0 : 1, manager.CloseCalls);
            manager.RejectSql = manager.FailClose = false;
            if (initiallyOpen) Assert.Equal(1L, Scalar(manager, "SELECT 1"));
            else AssertReleased(path);
            Invoke(manager, helper);
            Assert.Equal(initiallyOpen, manager.DataBaseOpen);
        });
    }

    /// <summary>Creates nonpooled connections with bounded lock waiting.</summary>
    /// <returns>The connection settings.</returns>
    private static SQLiteConnectionStringBuilder Builder() => new() { Pooling = false, DefaultTimeout = 1, BusyTimeout = 1, JournalMode = SQLiteJournalModeEnum.Delete };

    /// <summary>Runs a helper selected by its test index.</summary>
    /// <param name="manager">The manager.</param>
    /// <param name="helper">The helper index.</param>
    private static void Invoke(SQLiteManager manager, int helper)
    {
        if (helper == 0) manager.GetTableNames();
        else if (helper == 1) manager.Vacuum();
        else manager.Optimize();
    }

    /// <summary>Reads a scalar from a caller-owned connection.</summary>
    /// <param name="manager">The manager.</param>
    /// <param name="sql">The SQL query.</param>
    /// <returns>The scalar.</returns>
    private static object Scalar(SQLiteManager manager, string sql)
    {
        using var command = new SQLiteCommand(sql, manager.DbConnection);
        return command.ExecuteScalar();
    }

    /// <summary>Verifies file release before test cleanup without garbage collection or pool clearing.</summary>
    /// <param name="path">The database path.</param>
    private static void AssertReleased(string path)
    {
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>Provides an isolated temporary database and deterministic final cleanup.</summary>
    /// <param name="test">The test body.</param>
    private static void WithFile(Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sqlite-lifetime-{Guid.NewGuid():N}.db");
        try { test(path); }
        finally
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
                if (File.Exists(file)) File.Delete(file);
        }
    }

    /// <summary>Injects errors after provider acquisition and release.</summary>
    private sealed class ProbeManager : SQLiteManager
    {
        /// <summary>Creates a manager with nonpooled settings.</summary>
        /// <param name="path">The database path.</param>
        public ProbeManager(string path) : base(path, Builder()) { CloseCalls = 0; }
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public bool RejectSql { get; set; }
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public bool FailOpen { get; set; }
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public bool FailClose { get; set; }
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public int CloseCalls { get; private set; }
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public InvalidOperationException OpenError { get; } = new("partial open failure");
        /// <summary>Provides the injected failure or recorded lifecycle state.</summary>
        public InvalidOperationException CloseError { get; } = new("close failure");
        /// <summary>Opens the provider before injecting a failure.</summary>
        public override void Open() { base.Open(); if (FailOpen) throw OpenError; }
        /// <summary>Closes the provider before injecting a failure.</summary>
        public override void Close() { CloseCalls++; base.Close(); if (FailClose) throw CloseError; }
    }

    /// <summary>Injects initialization failure without shared static state.</summary>
    private sealed class FailingConstructorManager : SQLiteManager
    {
        // The constructor argument is unavailable during base initialization; AsyncLocal isolates the setup callback.
        private static readonly AsyncLocal<Action<SQLiteConnection>?> Failure = new();
        /// <summary>Sets the callback before base initialization.</summary>
        /// <param name="path">The database path.</param>
        /// <param name="fail">The failure callback.</param>
        /// <returns>The database path.</returns>
        private static string Prepare(string path, Action<SQLiteConnection> fail) { Failure.Value = fail; return path; }
        /// <summary>Invokes the ordinary constructor.</summary>
        /// <param name="path">The database path.</param>
        /// <param name="fail">The failure callback.</param>
        public FailingConstructorManager(string path, Action<SQLiteConnection> fail) : base(Prepare(path, fail), Builder()) { }
        /// <summary>Invokes the password constructor.</summary>
        /// <param name="path">The database path.</param>
        /// <param name="password">The password.</param>
        /// <param name="fail">The failure callback.</param>
        public FailingConstructorManager(string path, string password, Action<SQLiteConnection> fail) : base(Prepare(path, fail), password, Builder()) { }
        /// <summary>Throws during constructor initialization after provider acquisition.</summary>
        /// <returns>Never returns.</returns>
        public override string[] GetTableNames()
        {
            var callback = Failure.Value!;
            Failure.Value = null;
            callback(DbConnection);
            return [];
        }
        /// <summary>Rejects unsafe virtual disposal during base construction.</summary>
        /// <param name="disposing">Whether disposal is explicit.</param>
        protected override void Dispose(bool disposing) => throw new InvalidOperationException("virtual disposal during construction");
    }
}
