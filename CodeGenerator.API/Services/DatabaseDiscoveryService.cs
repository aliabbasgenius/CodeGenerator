using Microsoft.EntityFrameworkCore;
using CodeGenerator.API.Models;
using System.Data;

namespace CodeGenerator.API.Services
{
    public interface IDatabaseDiscoveryService
    {
        Task<List<DatabaseTable>> GetTablesAsync();
        Task<DatabaseTable> GetTableSchemaAsync(string tableName, string schema = "dbo");
        Task<bool> TestConnectionAsync();
    }

    public class DatabaseDiscoveryService : IDatabaseDiscoveryService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseDiscoveryService> _logger;

        public DatabaseDiscoveryService(IConfiguration configuration, ILogger<DatabaseDiscoveryService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentException("DefaultConnection string is required");
            _logger = logger;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database");
                return false;
            }
        }

        public async Task<List<DatabaseTable>> GetTablesAsync()
        {
            var tables = new List<DatabaseTable>();

            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        t.TABLE_SCHEMA as SchemaName,
                        t.TABLE_NAME as TableName
                    FROM INFORMATION_SCHEMA.TABLES t
                    WHERE t.TABLE_TYPE = 'BASE TABLE'
                    AND t.TABLE_SCHEMA != 'sys'
                    ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var table = new DatabaseTable
                    {
                        Schema = reader.GetString("SchemaName"),
                        TableName = reader.GetString("TableName")
                    };
                    
                    tables.Add(table);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve tables from database");
                throw;
            }

            // Load column information for each table
            foreach (var table in tables)
            {
                table.Columns = await GetTableColumnsAsync(table.TableName, table.Schema);
            }

            return tables;
        }

        public async Task<DatabaseTable> GetTableSchemaAsync(string tableName, string schema = "dbo")
        {
            var table = new DatabaseTable
            {
                TableName = tableName,
                Schema = schema,
                Columns = await GetTableColumnsAsync(tableName, schema)
            };

            return table;
        }

        private async Task<List<DatabaseColumn>> GetTableColumnsAsync(string tableName, string schema)
        {
            var columns = new List<DatabaseColumn>();

            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.IS_NULLABLE,
                        c.CHARACTER_MAXIMUM_LENGTH,
                        c.COLUMN_DEFAULT,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
                        CASE WHEN COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 1 ELSE 0 END as IsIdentity,
                        fk.REFERENCED_TABLE_NAME,
                        fk.REFERENCED_COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN (
                        SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                            ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                            AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                            AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                            AND tc.TABLE_NAME = ku.TABLE_NAME
                    ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                           AND c.TABLE_NAME = pk.TABLE_NAME 
                           AND c.COLUMN_NAME = pk.COLUMN_NAME
                    LEFT JOIN (
                        SELECT 
                            kcu.TABLE_SCHEMA,
                            kcu.TABLE_NAME,
                            kcu.COLUMN_NAME,
                            ccu.TABLE_NAME AS REFERENCED_TABLE_NAME,
                            ccu.COLUMN_NAME AS REFERENCED_COLUMN_NAME
                        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                            ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                            AND rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                        INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                            ON rc.UNIQUE_CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                            AND rc.UNIQUE_CONSTRAINT_SCHEMA = ccu.CONSTRAINT_SCHEMA
                    ) fk ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA 
                           AND c.TABLE_NAME = fk.TABLE_NAME 
                           AND c.COLUMN_NAME = fk.COLUMN_NAME
                    WHERE c.TABLE_NAME = @tableName 
                    AND c.TABLE_SCHEMA = @schema
                    ORDER BY c.ORDINAL_POSITION";

                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@schema", schema);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var column = new DatabaseColumn
                    {
                        ColumnName = reader.GetString("COLUMN_NAME"),
                        DataType = reader.GetString("DATA_TYPE"),
                        IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                        MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                        IsPrimaryKey = reader.GetInt32("IsPrimaryKey") == 1,
                        IsIdentity = reader.GetInt32("IsIdentity") == 1,
                        IsForeignKey = !reader.IsDBNull("REFERENCED_TABLE_NAME"),
                        ReferencedTable = reader.IsDBNull("REFERENCED_TABLE_NAME") ? null : reader.GetString("REFERENCED_TABLE_NAME"),
                        ReferencedColumn = reader.IsDBNull("REFERENCED_COLUMN_NAME") ? null : reader.GetString("REFERENCED_COLUMN_NAME")
                    };

                    columns.Add(column);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve columns for table {TableName}.{Schema}", tableName, schema);
                throw;
            }

            return columns;
        }
    }
}