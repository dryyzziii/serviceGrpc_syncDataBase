# Service gRPC de Synchronisation de Bases de Données

Ce projet est un service gRPC conçu pour synchroniser les données entre deux bases de données SQL via un JSON spécifiant les connexions, la requête SQL et les mappages de colonnes. Il est principalement compatible avec SQLite, PostgreSQL (via `Npgsql`), et les bases de données connectées via ODBC.

## Fonctionnalités

- Synchronisation des données entre une base de données source et une base de données cible.
- Configuration flexible des connexions, des requêtes SQL, et des mappages de colonnes.
- Supporte les bases de données compatibles avec ODBC et PostgreSQL.

## Prérequis

- .NET 6.0 ou supérieur
- Paquets NuGet :
  - `Grpc.AspNetCore`
  - `Microsoft.Data.Sqlite`
  - `Npgsql`
  - `Dapper`
- Serveur de bases de données SQL (par exemple PostgreSQL) avec les DSN configurés pour ODBC si nécessaire.

## Utilisation

Pour utiliser le service gRPC de synchronisation, vous devez fournir un JSON contenant les informations de connexion et de synchronisation. Ce JSON précise la base de données source, la base de données cible, la requête SQL, et les mappages de colonnes.

### Structure du JSON d'entrée

Le JSON d'entrée doit inclure les éléments suivants :

- **`source_connection_string`** : Chaîne de connexion pour accéder à la base de données source (ex : DSN ODBC).
- **`target_connection_string`** : Chaîne de connexion pour accéder à la base de données cible.
- **`sqlRequest`** : Requête SQL pour sélectionner les données à synchroniser depuis la base source.
- **`database_type`** : Type de base de données (par exemple, `odbc` ou `postgresql`).
- **`destination_table`** : Nom de la table cible dans laquelle les données seront insérées ou mises à jour.
- **`unique_column`** : Nom de la colonne unique dans la table cible qui permet d'identifier chaque ligne.
- **`columns`** : Liste des colonnes à synchroniser avec les mappages suivants :
  - **`source_column`** : Nom de la colonne dans la table source.
  - **`target_column`** : Nom de la colonne correspondante dans la table cible.

### Exemple de JSON d'entrée

Voici un exemple de JSON que vous pouvez utiliser pour une synchronisation de données :

```json
{
  "source_connection_string": "DSN=DSN64source_db;",
  "target_connection_string": "DSN=DSN64target_db;",
  "sqlRequest": "SELECT id, name, updateddate FROM public.testtable",
  "database_type": "odbc",
  "destination_table": "public.testtable",
  "unique_column": "id",
  "columns": [
    { "source_column": "id", "target_column": "id" },
    { "source_column": "name", "target_column": "name" },
    { "source_column": "updateddate", "target_column": "updateddate" }
  ]
}


---