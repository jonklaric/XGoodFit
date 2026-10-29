using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DatabaseManager;

// ProjectProbe opens .bestfit project files through the upstream DatabaseManager/SQLite stack and
// prints a fingerprint per project, so the same files can be compared across operating systems.
//
// Usage: ProjectProbe <folder-or-.bestfit-file> [--tables]
//
// Every table is read twice: through the upstream DataTableView (the path the app uses, with its
// type conversions) and through raw SQL. The fingerprint hashes the raw stored values as rendered
// by SQLite's quote(), so it does not depend on culture or time zone.

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.Error.WriteLine("Usage: ProjectProbe <folder-or-.bestfit-file> [--tables]");
    return 2;
}

// The output is meant to be compared across operating systems, so use the same line ending everywhere.
Console.Out.NewLine = "\n";

string target = Path.GetFullPath(args[0]);
bool showTables = args.Contains("--tables");
string baseDir = File.Exists(target) ? Path.GetDirectoryName(target)! : target;
string[] files = File.Exists(target)
    ? [target]
    : Directory.GetFiles(target, "*.bestfit", SearchOption.AllDirectories);
Array.Sort(files, (a, b) => string.CompareOrdinal(RelativeName(a), RelativeName(b)));

Console.WriteLine($"# {RuntimeInformation.OSDescription} | {RuntimeInformation.ProcessArchitecture} | .NET {Environment.Version} | " +
    $"System.Data.SQLite {typeof(SQLiteConnection).Assembly.GetName().Version} | SQLite {SQLiteConnection.SQLiteVersion}");

// Projects are opened from temporary copies: the upstream default connection switches databases
// to WAL mode, which would otherwise rewrite the fixtures.
string tempRoot = Directory.CreateTempSubdirectory("xgoodfit-probe-").FullName;
int failures = 0;
try
{
    foreach (string file in files)
    {
        string name = RelativeName(file);
        try
        {
            string copy = Path.Combine(tempRoot, Path.GetRandomFileName() + ".bestfit");
            File.Copy(file, copy);
            ProjectFingerprint project = Probe(copy);
            Console.WriteLine($"{name}\ttables={project.Tables.Count}\trows={project.TotalRows}\tsha256={project.Hash}");
            if (showTables)
            {
                foreach (TableFingerprint table in project.Tables)
                {
                    Console.WriteLine($"    {table.Name}\tcolumns={table.Columns}\trows={table.Rows}\tsha256={table.Hash}");
                }
            }
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine($"{name}\tFAILED\t{ex.GetType().Name}: {ex.Message}");
        }
    }
}
finally
{
    SQLiteConnection.ClearAllPools();
    try
    {
        Directory.Delete(tempRoot, recursive: true);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Warning: could not delete {tempRoot}: {ex.Message}");
    }
}

Console.WriteLine($"# {files.Length} projects, {failures} failed");
return failures == 0 ? 0 : 1;

string RelativeName(string path) => Path.GetRelativePath(baseDir, path).Replace('\\', '/');

static ProjectFingerprint Probe(string file)
{
    var tables = new List<TableFingerprint>();
    using var project = new SQLiteManager(file);
    project.Open();
    try
    {
        string[] names = project.GetTableNames();
        Array.Sort(names, StringComparer.Ordinal);
        foreach (string name in names)
        {
            int viewRows = ReadThroughView(project, name);
            tables.Add(HashTable(project.DbConnection, name, viewRows));
        }
    }
    finally
    {
        project.Close();
    }

    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (TableFingerprint table in tables)
    {
        hash.AppendData(Encoding.UTF8.GetBytes($"{table.Name}:{table.Hash}\n"));
    }
    return new ProjectFingerprint(tables, tables.Sum(t => t.Rows), Hex(hash.GetHashAndReset()));
}

// Loads every row through the same DataTableView the app uses, so type conversion runs as in RMC-BestFit.
static int ReadThroughView(SQLiteManager project, string table)
{
    const int chunk = 5000;
    using DataTableView view = project.GetTableManager(table);
    for (int start = 0; start < view.NumberOfRows; start += chunk)
    {
        view.GetRows(start, Math.Min(start + chunk, view.NumberOfRows) - 1);
    }
    return view.NumberOfRows;
}

static TableFingerprint HashTable(SQLiteConnection connection, string table, int viewRows)
{
    var columns = new List<string>();
    using (var info = new SQLiteCommand($"PRAGMA table_info({Quote(table)})", connection))
    using (SQLiteDataReader reader = info.ExecuteReader())
    {
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
    }

    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    hash.AppendData(Encoding.UTF8.GetBytes($"{table}\n{string.Join('\u001f', columns)}\n"));
    long rows = 0;
    if (columns.Count > 0)
    {
        string values = string.Join(" || char(31) || ", columns.Select(c => $"quote({Quote(c)})"));
        using var select = new SQLiteCommand($"SELECT {values} FROM {Quote(table)}", connection);
        using SQLiteDataReader reader = select.ExecuteReader();
        while (reader.Read())
        {
            hash.AppendData(Encoding.UTF8.GetBytes(reader.GetString(0) + "\n"));
            rows++;
        }
    }

    if (rows != viewRows)
    {
        throw new InvalidDataException($"Table '{table}': raw SQL returned {rows} rows but the DataTableView reported {viewRows}.");
    }
    return new TableFingerprint(table, columns.Count, rows, Hex(hash.GetHashAndReset()));
}

static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

static string Hex(byte[] bytes) => Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();

record TableFingerprint(string Name, int Columns, long Rows, string Hash);

record ProjectFingerprint(IReadOnlyList<TableFingerprint> Tables, long TotalRows, string Hash);
