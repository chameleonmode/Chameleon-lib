using System.Data;
using System.Data.SQLite;
using Chameleon.lib.Util;

namespace Chameleon.lib.Storage;

/// <summary>
/// A simple storage service that uses SQLite as a database backend.
/// </summary>
public class SqliteStorageService
{
  private readonly string connectionString;

  /// <summary>
  /// Initializes a new instance of the SQLite storage service.
  /// </summary>
  /// <param name="dbPath">Path to the SQLite database file.</param>
  private SqliteStorageService()
  {
    var dbPath = Path.Combine(FilePaths.AppDataDir, "chameleon.db");
    connectionString = $"Data Source=\"{dbPath}\";Version=3;";

    // Create the database file if it doesn't exist
    if (!File.Exists(dbPath))
    {
      SQLiteConnection.CreateFile(dbPath);
    }
  }

  /// <summary>
  /// Creates a new table in the database if it doesn't exist.
  /// </summary>
  /// <param name="tableName">Name of the table to create.</param>
  /// <param name="columns">Dictionary of column names and their SQL types.</param>
  /// <param name="dropIfExists">Whether to drop the table if it already exists.</param>
  public void CreateTable(string tableName, Dictionary<string, string> columns, bool dropIfExists = false)
  {
    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentException("Table name cannot be null or empty.");

    if (columns == null || columns.Count == 0)
      throw new ArgumentException("Columns dictionary cannot be null or empty.");

    var columnsDefinition = string.Empty;
    foreach (var column in columns)
    {
      columnsDefinition += $"{column.Key} {column.Value}, ";
    }

    // Remove the trailing comma and space
    columnsDefinition = columnsDefinition.Substring(0, columnsDefinition.Length - 2);

    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();

    if (dropIfExists)
    {
      command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
      command.ExecuteNonQuery();
    }

    command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({columnsDefinition})";
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Inserts a new record into the specified table.
  /// </summary>
  /// <param name="tableName">Name of the table to insert into.</param>
  /// <param name="values">Dictionary of column names and their values.</param>
  /// <returns>The ID of the newly inserted record.</returns>
  public long Insert(string tableName, Dictionary<string, object> values)
  {
    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentException("Table name cannot be null or empty.");

    if (values == null || values.Count == 0)
      throw new ArgumentException("Values dictionary cannot be null or empty.");

    var columns = string.Empty;
    var parameters = string.Empty;

    foreach (var value in values)
    {
      columns += $"{value.Key}, ";
      parameters += $"@{value.Key}, ";
    }

    // Remove the trailing comma and space
    columns = columns[..^2];
    parameters = parameters[..^2];

    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

    foreach (var value in values)
    {
      _ = command.Parameters.AddWithValue($"@{value.Key}", value.Value ?? DBNull.Value);
    }

    _ = command.ExecuteNonQuery();

    // Get the ID of the last inserted row
    command.CommandText = "SELECT last_insert_rowid()";
    return (long)command.ExecuteScalar();
  }


  /// <summary>
  /// Updates an existing record in the specified table.
  /// </summary>
  /// <param name="tableName">Name of the table to update.</param>
  /// <param name="values">Dictionary of column names and their new values.</param>
  /// <param name="whereClause">WHERE clause for the update statement. If null, updates all rows.</param>
  /// <param name="whereParams">Parameters for the WHERE clause. If null, no parameters are used.</param>
  /// <returns>The number of rows affected.</returns>
  public int Update(string tableName, Dictionary<string, object> values, string? whereClause = null, Dictionary<string, object>? whereParams = null)
  {
    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentException("Table name cannot be null or empty.");

    if (values == null || values.Count == 0)
      throw new ArgumentException("Values dictionary cannot be null or empty.");

    var setClause = string.Empty;
    foreach (var value in values)
    {
      setClause += $"{value.Key} = @{value.Key}, ";
    }

    // Remove the trailing comma and space
    setClause = setClause.Substring(0, setClause.Length - 2);

    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      var command = connection.CreateCommand();
      command.CommandText = string.IsNullOrEmpty(whereClause)
          ? $"UPDATE {tableName} SET {setClause}"
          : $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";

      foreach (var value in values)
      {
        command.Parameters.AddWithValue($"@{value.Key}", value.Value ?? DBNull.Value);
      }

      if (whereParams != null)
      {
        foreach (var param in whereParams)
        {
          command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
        }
      }

      return command.ExecuteNonQuery();
    }
  }

  /// <summary>
  /// Deletes records from the specified table.
  /// </summary>
  /// <param name="tableName">Name of the table to delete from.</param>
  /// <param name="whereClause">WHERE clause for the delete statement.</param>
  /// <param name="whereParams">Parameters for the WHERE clause.</param>
  /// <returns>The number of rows affected.</returns>
  public int Delete(string tableName, string whereClause, Dictionary<string, object> whereParams)
  {
    if (string.IsNullOrEmpty(tableName))
      throw new ArgumentException("Table name cannot be null or empty.");

    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = $"DELETE FROM {tableName} WHERE {whereClause}";

    if (whereParams != null)
    {
      foreach (var param in whereParams)
      {
        _ = command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
      }
    }

    return command.ExecuteNonQuery();
  }

  /// <summary>
  /// Executes a SELECT query and returns the result as a DataTable.
  /// </summary>
  /// <param name="query">The SQL query to execute.</param>
  /// <param name="parameters">Parameters for the query.</param>
  /// <returns>A DataTable containing the query results.</returns>
  public DataTable Query(string query, Dictionary<string, object>? parameters = null)
  {
    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = query;

    if (parameters != null)
    {
      foreach (var param in parameters)
      {
        _ = command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
      }
    }

    var adapter = new SQLiteDataAdapter(command);
    var dataTable = new DataTable();
    _ = adapter.Fill(dataTable);

    return dataTable;
  }

  /// <summary>
  /// Executes a custom SQL command.
  /// </summary>
  /// <param name="sql">The SQL command to execute.</param>
  /// <param name="parameters">Parameters for the command.</param>
  /// <returns>The number of rows affected.</returns>
  public int ExecuteCommand(string sql, Dictionary<string, object>? parameters = null)
  {
    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = sql;

    if (parameters != null)
    {
      foreach (var param in parameters)
      {
        _ = command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
      }
    }

    return command.ExecuteNonQuery();
  }

  /// <summary>
  /// Checks if a table exists in the database.
  /// </summary>
  /// <param name="tableName">Name of the table to check.</param>
  /// <returns>True if the table exists, otherwise false.</returns>
  public bool TableExists(string tableName)
  {
    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
    _ = command.Parameters.AddWithValue("@tableName", tableName);

    var result = command.ExecuteScalar();
    return result != null;
  }

  /// <summary>
  /// Begins a transaction.
  /// </summary>
  /// <returns>An SQLiteTransaction object.</returns>
  public SQLiteTransaction BeginTransaction()
  {
    var connection = new SQLiteConnection(connectionString);
    connection.Open();
    return connection.BeginTransaction();
  }

  // Singleton instance
  private static SqliteStorageService? instance;
  public static SqliteStorageService Instance => instance ??= new SqliteStorageService();
}

