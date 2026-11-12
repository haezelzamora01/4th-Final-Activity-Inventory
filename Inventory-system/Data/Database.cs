using System.Data;
using System.Data.OleDb;

namespace InventoryManagementSystem.Data;

public static class Database
{
    private const string Provider = "Microsoft.ACE.OLEDB.12.0";

    private static string DatabasePath
    {
        get
        {
            string outputDatabasePath = Path.Combine(
                AppContext.BaseDirectory,
                "Database",
                "InventoryDatabase.accdb");

            string projectDatabasePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",
                "Database",
                "InventoryDatabase.accdb"));

            return File.Exists(projectDatabasePath) ? projectDatabasePath : outputDatabasePath;
        }
    }

    private static string ConnectionString =>
        $"Provider={Provider};Data Source={DatabasePath};Persist Security Info=False;";

    public static OleDbConnection GetConnection()
    {
        if (!File.Exists(DatabasePath))
        {
            throw new FileNotFoundException(
                "The Access database was not found. Make sure Database\\InventoryDatabase.accdb is included in the output directory.",
                DatabasePath);
        }

        try
        {
            return new OleDbConnection(ConnectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The Microsoft Access Database Engine could not be initialized. Install the ACE provider matching the application's platform target.",
                ex);
        }
    }

    public static OleDbConnection OpenConnection()
    {
        OleDbConnection connection = GetConnection();
        try
        {
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            connection.Dispose();
            throw new InvalidOperationException(
                "The Access database connection could not be opened. Verify that the Microsoft Access Database Engine is installed and that its 32-bit or 64-bit platform matches this application.",
                ex);
        }
    }

    public static void TestConnection()
    {
        using OleDbConnection connection = OpenConnection();
    }

    public static void EnsureProductCreatedAtColumn()
    {
        using OleDbConnection connection = OpenConnection();

        EnsureColumn(connection, "CreatedAt", "DATETIME");
        EnsureColumn(connection, "Description", "TEXT(255)");

        using OleDbCommand fillMissingDates = connection.CreateCommand();
        fillMissingDates.CommandText = "UPDATE Products SET CreatedAt = ? WHERE CreatedAt IS NULL";
        fillMissingDates.Parameters.Add(new OleDbParameter("@createdAt", OleDbType.Date) { Value = DateTime.Now });
        fillMissingDates.ExecuteNonQuery();
    }

    private static void EnsureColumn(OleDbConnection connection, string columnName, string columnDefinition)
    {
        try
        {
            using OleDbCommand check = connection.CreateCommand();
            check.CommandText = $"SELECT [{columnName}] FROM Products WHERE 1 = 0";
            check.ExecuteScalar();
        }
        catch (OleDbException)
        {
            using OleDbCommand addColumn = connection.CreateCommand();
            addColumn.CommandText = $"ALTER TABLE Products ADD COLUMN [{columnName}] {columnDefinition}";
            addColumn.ExecuteNonQuery();
        }
    }

    public static DataTable GetSchemaTables()
    {
        using OleDbConnection connection = OpenConnection();
        return connection.GetSchema("Tables");
    }
}
