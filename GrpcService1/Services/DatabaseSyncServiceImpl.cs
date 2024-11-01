using Grpc.Core;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Odbc;
using System.Data;
using Dapper;

namespace GrpcService1.Services
{
    public class DatabaseSyncServiceImpl : DatabaseSyncService.DatabaseSyncServiceBase
    {
        private readonly ILogger<DatabaseSyncServiceImpl> _logger;

        public DatabaseSyncServiceImpl(ILogger<DatabaseSyncServiceImpl> logger)
        {
            _logger = logger;
        }

        static DatabaseSyncServiceImpl()
        {
            SQLitePCL.Batteries.Init();
        }

        public override async Task<SyncResponse> SynchronizeDatabases(SyncRequest request, ServerCallContext context)
        {
            bool success = true;
            string message;

            try
            {
                using var sourceConnection = CreateConnection(request.SourceConnectionString, request.DatabaseType);
                using var targetConnection = CreateConnection(request.TargetConnectionString, request.DatabaseType);

                sourceConnection.Open();
                targetConnection.Open();

                await SynchronizeTablesAsync(sourceConnection, targetConnection, request.SqlRequest, request.DestinationTable, request.Columns, request.UniqueColumn);

                message = "Synchronisation réussie de la base source vers la base cible.";
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Erreur durant la synchronisation : {ex.Message}";
            }

            return new SyncResponse
            {
                Success = success,
                Message = message
            };
        }

        private IDbConnection CreateConnection(string connectionString, string databaseType)
        {
            return databaseType.ToLower() switch
            {
                "sqlite" => new SqliteConnection(connectionString),
                "postgresql" => new NpgsqlConnection(connectionString),
                "odbc" => new OdbcConnection(connectionString),
                _ => throw new ArgumentException("Type de base de données non supporté")
            };
        }

        private async Task SynchronizeTablesAsync(IDbConnection sourceConnection, IDbConnection targetConnection, string sqlRequest, string destinationTable, IEnumerable<ColumnMapping> columns, string uniqueColumn)
        {
            var dataToSync = await sourceConnection.QueryAsync(sqlRequest);

            foreach (var row in dataToSync)
            {
                // Construire les parties de la requête SQL en fonction des colonnes spécifiées
                var columnNames = string.Join(", ", columns.Select(c => c.TargetColumn));
                var parameterPlaceholders = string.Join(", ", columns.Select(c => "?"));
                var updateAssignments = string.Join(", ", columns.Select(c => $"{c.TargetColumn} = ?"));

                // Préparer les valeurs des colonnes source
                var columnValues = columns.ToDictionary(c => c.TargetColumn, c => ((IDictionary<string, object>)row)[c.SourceColumn]);

                // Récupérer la valeur de la colonne unique spécifiée pour les conditions WHERE
                var uniqueValue = ((IDictionary<string, object>)row)[uniqueColumn];

                // Vérifier si l'enregistrement existe déjà dans la table de destination
                var existsQuery = $"SELECT COUNT(1) FROM {destinationTable} WHERE {uniqueColumn} = ?";
                var exists = await targetConnection.ExecuteScalarAsync<int>(existsQuery, new { uniqueValue }) > 0;

                if (exists)
                {
                    // Mise à jour de l'enregistrement existant avec les colonnes spécifiées
                    var updateQuery = $"UPDATE {destinationTable} SET {updateAssignments} WHERE {uniqueColumn} = ?";
                    var updateParams = new DynamicParameters();

                    foreach (var column in columns)
                    {
                        updateParams.Add(column.TargetColumn, columnValues[column.TargetColumn]);
                    }

                    updateParams.Add("uniqueValueForWhere", uniqueValue);

                    await targetConnection.ExecuteAsync(updateQuery, updateParams);
                }
                else
                {
                    // Insertion d'un nouvel enregistrement avec les colonnes spécifiées
                    var insertQuery = $"INSERT INTO {destinationTable} ({columnNames}) VALUES ({parameterPlaceholders})";
                    var insertParams = new DynamicParameters();

                    foreach (var column in columns)
                    {
                        insertParams.Add(column.TargetColumn, columnValues[column.TargetColumn]);
                    }

                   // insertParams.Add(uniqueColumn, uniqueValue);

                    await targetConnection.ExecuteAsync(insertQuery, insertParams);
                }
            }
        }

    }
}
