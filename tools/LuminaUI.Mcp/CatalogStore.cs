using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using LuminaUI.Mcp.Models;

namespace LuminaUI.Mcp;

public sealed class CatalogStore : IDisposable
{
    private static readonly Regex SearchTermRegex = new(@"[\p{L}\p{N}_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _dbPath;
    private readonly object _gate = new();
    private SqliteConnection? _connection;
    private CatalogVersion? _currentVersion;

    public CatalogStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    public CatalogVersion? CurrentVersion
    {
        get
        {
            lock (_gate)
                return _currentVersion?.Clone();
        }
    }

    public void Initialize()
    {
        lock (_gate)
        {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };

            _connection = new SqliteConnection(builder.ToString());
            _connection.Open();

            ExecuteNonQuery("PRAGMA busy_timeout = 5000;");
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");

            using var cmd = CreateCommand("""
                CREATE TABLE IF NOT EXISTS catalog_versions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    library_version TEXT NOT NULL,
                    source_commit TEXT,
                    generated_at TEXT NOT NULL,
                    component_count INTEGER NOT NULL,
                    enum_count INTEGER NOT NULL,
                    example_count INTEGER NOT NULL,
                    token_count INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS components (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    namespace TEXT NOT NULL,
                    assembly TEXT NOT NULL,
                    base_type TEXT NOT NULL,
                    category TEXT,
                    description TEXT,
                    axaml_namespace TEXT,
                    properties_json TEXT NOT NULL,
                    style_classes_json TEXT NOT NULL,
                    theme_resources_json TEXT NOT NULL
                );

                CREATE VIRTUAL TABLE IF NOT EXISTS components_fts USING fts5(
                    name, namespace, base_type, category, description,
                    content='components', content_rowid='id'
                );

                CREATE TABLE IF NOT EXISTS enums (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    namespace TEXT NOT NULL,
                    description TEXT,
                    values_json TEXT NOT NULL
                );

                CREATE VIRTUAL TABLE IF NOT EXISTS enums_fts USING fts5(
                    name, namespace, description,
                    content='enums', content_rowid='id'
                );

                CREATE TABLE IF NOT EXISTS examples (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    component_name TEXT NOT NULL,
                    showcase_page TEXT NOT NULL,
                    axaml_source TEXT,
                    code_behind_source TEXT,
                    view_model_source TEXT
                );

                CREATE VIRTUAL TABLE IF NOT EXISTS examples_fts USING fts5(
                    component_name, showcase_page, axaml_source,
                    content='examples', content_rowid='id'
                );

                CREATE TABLE IF NOT EXISTS design_tokens (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    category TEXT NOT NULL,
                    light_value TEXT,
                    dark_value TEXT,
                    description TEXT
                );

                CREATE VIRTUAL TABLE IF NOT EXISTS tokens_fts USING fts5(
                    name, category, description,
                    content='design_tokens', content_rowid='id'
                );
                """);
            cmd.ExecuteNonQuery();

            LoadCurrentVersion();
        }
    }

    private void LoadCurrentVersion()
    {
        if (_connection is null) return;
        using var cmd = CreateCommand("SELECT id, library_version, source_commit, generated_at, component_count FROM catalog_versions ORDER BY id DESC LIMIT 1");
        using var reader = cmd.ExecuteReader();
        _currentVersion = reader.Read()
            ? new CatalogVersion
            {
                Id = reader.GetInt64(0),
                LibraryVersion = reader.GetString(1),
                SourceCommit = reader.IsDBNull(2) ? null : reader.GetString(2),
                GeneratedAt = reader.GetString(3),
                ComponentCount = reader.GetInt32(4)
            }
            : null;
    }

    public void SaveCatalog(CatalogData data)
    {
        lock (_gate)
        {
            if (_connection is null) throw new InvalidOperationException("Store not initialized");

            using var transaction = _connection.BeginTransaction();

            ExecuteNonQuery("DELETE FROM catalog_versions", transaction);
            ExecuteNonQuery("DELETE FROM components", transaction);
            ExecuteNonQuery("DELETE FROM components_fts", transaction);
            ExecuteNonQuery("DELETE FROM enums", transaction);
            ExecuteNonQuery("DELETE FROM enums_fts", transaction);
            ExecuteNonQuery("DELETE FROM examples", transaction);
            ExecuteNonQuery("DELETE FROM examples_fts", transaction);
            ExecuteNonQuery("DELETE FROM design_tokens", transaction);
            ExecuteNonQuery("DELETE FROM tokens_fts", transaction);

            using (var cmd = CreateCommand("""
                INSERT INTO catalog_versions (library_version, source_commit, generated_at, component_count, enum_count, example_count, token_count)
                VALUES ($ver, $commit, $at, $comp, $enum, $ex, $tok)
                """, transaction))
            {
                cmd.Parameters.AddWithValue("$ver", data.LibraryVersion);
                cmd.Parameters.AddWithValue("$commit", (object?)data.SourceCommit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("$comp", data.Components.Count);
                cmd.Parameters.AddWithValue("$enum", data.Enums.Count);
                cmd.Parameters.AddWithValue("$ex", data.Examples.Count);
                cmd.Parameters.AddWithValue("$tok", data.DesignTokens.Count);
                cmd.ExecuteNonQuery();
            }

            foreach (var component in data.Components)
            {
                using var cmd = CreateCommand("""
                    INSERT INTO components (name, namespace, assembly, base_type, category, description, axaml_namespace, properties_json, style_classes_json, theme_resources_json)
                    VALUES ($name, $ns, $asm, $base, $cat, $desc, $axns, $props, $styles, $themes)
                    """, transaction);
                cmd.Parameters.AddWithValue("$name", component.Name);
                cmd.Parameters.AddWithValue("$ns", component.Namespace);
                cmd.Parameters.AddWithValue("$asm", component.Assembly);
                cmd.Parameters.AddWithValue("$base", component.BaseType);
                cmd.Parameters.AddWithValue("$cat", (object?)component.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$desc", (object?)component.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$axns", (object?)component.AxamlNamespace ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$props", JsonSerializer.Serialize(component.Properties));
                cmd.Parameters.AddWithValue("$styles", JsonSerializer.Serialize(component.StyleClasses));
                cmd.Parameters.AddWithValue("$themes", JsonSerializer.Serialize(component.ThemeResources));
                cmd.ExecuteNonQuery();

                using var ftsCmd = CreateCommand("INSERT INTO components_fts(rowid, name, namespace, base_type, category, description) VALUES (last_insert_rowid(), $name, $ns, $base, $cat, $desc)", transaction);
                ftsCmd.Parameters.AddWithValue("$name", component.Name);
                ftsCmd.Parameters.AddWithValue("$ns", component.Namespace);
                ftsCmd.Parameters.AddWithValue("$base", component.BaseType);
                ftsCmd.Parameters.AddWithValue("$cat", (object?)component.Category ?? DBNull.Value);
                ftsCmd.Parameters.AddWithValue("$desc", (object?)component.Description ?? DBNull.Value);
                ftsCmd.ExecuteNonQuery();
            }

            foreach (var enumInfo in data.Enums)
            {
                using var cmd = CreateCommand("INSERT INTO enums (name, namespace, description, values_json) VALUES ($name, $ns, $desc, $vals)", transaction);
                cmd.Parameters.AddWithValue("$name", enumInfo.Name);
                cmd.Parameters.AddWithValue("$ns", enumInfo.Namespace);
                cmd.Parameters.AddWithValue("$desc", (object?)enumInfo.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$vals", JsonSerializer.Serialize(enumInfo.Values));
                cmd.ExecuteNonQuery();

                using var ftsCmd = CreateCommand("INSERT INTO enums_fts(rowid, name, namespace, description) VALUES (last_insert_rowid(), $name, $ns, $desc)", transaction);
                ftsCmd.Parameters.AddWithValue("$name", enumInfo.Name);
                ftsCmd.Parameters.AddWithValue("$ns", enumInfo.Namespace);
                ftsCmd.Parameters.AddWithValue("$desc", (object?)enumInfo.Description ?? DBNull.Value);
                ftsCmd.ExecuteNonQuery();
            }

            foreach (var example in data.Examples)
            {
                using var cmd = CreateCommand("INSERT INTO examples (component_name, showcase_page, axaml_source, code_behind_source, view_model_source) VALUES ($comp, $page, $axaml, $cb, $vm)", transaction);
                cmd.Parameters.AddWithValue("$comp", example.ComponentName);
                cmd.Parameters.AddWithValue("$page", example.ShowcasePage);
                cmd.Parameters.AddWithValue("$axaml", (object?)example.AxamlSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$cb", (object?)example.CodeBehindSource ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$vm", (object?)example.ViewModelSource ?? DBNull.Value);
                cmd.ExecuteNonQuery();

                using var ftsCmd = CreateCommand("INSERT INTO examples_fts(rowid, component_name, showcase_page, axaml_source) VALUES (last_insert_rowid(), $comp, $page, $axaml)", transaction);
                ftsCmd.Parameters.AddWithValue("$comp", example.ComponentName);
                ftsCmd.Parameters.AddWithValue("$page", example.ShowcasePage);
                ftsCmd.Parameters.AddWithValue("$axaml", (object?)example.AxamlSource ?? DBNull.Value);
                ftsCmd.ExecuteNonQuery();
            }

            foreach (var token in data.DesignTokens)
            {
                using var cmd = CreateCommand("INSERT INTO design_tokens (name, category, light_value, dark_value, description) VALUES ($name, $cat, $light, $dark, $desc)", transaction);
                cmd.Parameters.AddWithValue("$name", token.Name);
                cmd.Parameters.AddWithValue("$cat", token.Category);
                cmd.Parameters.AddWithValue("$light", (object?)token.LightValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$dark", (object?)token.DarkValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$desc", (object?)token.Description ?? DBNull.Value);
                cmd.ExecuteNonQuery();

                using var ftsCmd = CreateCommand("INSERT INTO tokens_fts(rowid, name, category, description) VALUES (last_insert_rowid(), $name, $cat, $desc)", transaction);
                ftsCmd.Parameters.AddWithValue("$name", token.Name);
                ftsCmd.Parameters.AddWithValue("$cat", token.Category);
                ftsCmd.Parameters.AddWithValue("$desc", (object?)token.Description ?? DBNull.Value);
                ftsCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            LoadCurrentVersion();
        }
    }

    public List<ComponentInfo> SearchComponents(string query, int limit = 20)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            var normalizedLimit = NormalizeLimit(limit);
            var ftsQuery = BuildFtsQuery(query);
            if (ftsQuery is not null)
            {
                try
                {
                    using var cmd = CreateCommand("""
                        SELECT c.name, c.namespace, c.assembly, c.base_type, c.category, c.description, c.axaml_namespace, c.properties_json, c.style_classes_json, c.theme_resources_json
                        FROM components c
                        JOIN components_fts f ON c.id = f.rowid
                        WHERE components_fts MATCH $query
                        ORDER BY rank
                        LIMIT $limit
                        """);
                    cmd.Parameters.AddWithValue("$query", ftsQuery);
                    cmd.Parameters.AddWithValue("$limit", normalizedLimit);
                    var results = ReadComponents(cmd);
                    if (results.Count > 0) return results;
                }
                catch (SqliteException)
                {
                }
            }

            return SearchComponentsLike(query, normalizedLimit);
        }
    }

    public List<ComponentInfo> ListComponents(string? category = null, int limit = 100)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            using var cmd = CreateCommand(string.IsNullOrEmpty(category)
                ? "SELECT name, namespace, assembly, base_type, category, description, axaml_namespace, properties_json, style_classes_json, theme_resources_json FROM components ORDER BY name LIMIT $limit"
                : "SELECT name, namespace, assembly, base_type, category, description, axaml_namespace, properties_json, style_classes_json, theme_resources_json FROM components WHERE category = $cat ORDER BY name LIMIT $limit");

            if (!string.IsNullOrEmpty(category))
                cmd.Parameters.AddWithValue("$cat", category);

            cmd.Parameters.AddWithValue("$limit", NormalizeLimit(limit, 500));
            return ReadComponents(cmd);
        }
    }

    public ComponentInfo? GetComponent(string name)
    {
        lock (_gate)
        {
            if (_connection is null) return null;
            using var cmd = CreateCommand("SELECT name, namespace, assembly, base_type, category, description, axaml_namespace, properties_json, style_classes_json, theme_resources_json FROM components WHERE name = $name");
            cmd.Parameters.AddWithValue("$name", name);
            return ReadComponents(cmd).FirstOrDefault();
        }
    }

    public List<EnumInfo> SearchEnums(string query, int limit = 20)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            var normalizedLimit = NormalizeLimit(limit);
            var ftsQuery = BuildFtsQuery(query);
            if (ftsQuery is not null)
            {
                try
                {
                    using var cmd = CreateCommand("""
                        SELECT e.name, e.namespace, e.description, e.values_json
                        FROM enums e
                        JOIN enums_fts f ON e.id = f.rowid
                        WHERE enums_fts MATCH $query
                        ORDER BY rank
                        LIMIT $limit
                        """);
                    cmd.Parameters.AddWithValue("$query", ftsQuery);
                    cmd.Parameters.AddWithValue("$limit", normalizedLimit);
                    var results = ReadEnums(cmd);
                    if (results.Count > 0) return results;
                }
                catch (SqliteException)
                {
                }
            }

            return SearchEnumsLike(query, normalizedLimit);
        }
    }

    public EnumInfo? GetEnum(string name)
    {
        lock (_gate)
        {
            if (_connection is null) return null;
            using var cmd = CreateCommand("SELECT name, namespace, description, values_json FROM enums WHERE name = $name");
            cmd.Parameters.AddWithValue("$name", name);
            return ReadEnums(cmd).FirstOrDefault();
        }
    }

    public List<ExampleInfo> SearchExamples(string query, int limit = 10)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            var normalizedLimit = NormalizeLimit(limit);
            var ftsQuery = BuildFtsQuery(query);
            if (ftsQuery is not null)
            {
                try
                {
                    using var cmd = CreateCommand("""
                        SELECT e.component_name, e.showcase_page, e.axaml_source, e.code_behind_source, e.view_model_source
                        FROM examples e
                        JOIN examples_fts f ON e.id = f.rowid
                        WHERE examples_fts MATCH $query
                        ORDER BY rank
                        LIMIT $limit
                        """);
                    cmd.Parameters.AddWithValue("$query", ftsQuery);
                    cmd.Parameters.AddWithValue("$limit", normalizedLimit);
                    var results = ReadExamples(cmd);
                    if (results.Count > 0) return results;
                }
                catch (SqliteException)
                {
                }
            }

            return SearchExamplesLike(query, normalizedLimit);
        }
    }

    public ExampleInfo? GetExample(string componentName)
    {
        lock (_gate)
        {
            if (_connection is null) return null;
            using var cmd = CreateCommand("SELECT component_name, showcase_page, axaml_source, code_behind_source, view_model_source FROM examples WHERE component_name = $name");
            cmd.Parameters.AddWithValue("$name", componentName);
            return ReadExamples(cmd).FirstOrDefault();
        }
    }

    public List<ExampleInfo> GetExamplesByShowcase(string showcasePage)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            using var cmd = CreateCommand("SELECT component_name, showcase_page, axaml_source, code_behind_source, view_model_source FROM examples WHERE showcase_page = $page");
            cmd.Parameters.AddWithValue("$page", showcasePage);
            return ReadExamples(cmd);
        }
    }

    public List<DesignToken> SearchTokens(string query, int limit = 30)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            var normalizedLimit = NormalizeLimit(limit);
            var ftsQuery = BuildFtsQuery(query);
            if (ftsQuery is not null)
            {
                try
                {
                    using var cmd = CreateCommand("""
                        SELECT t.name, t.category, t.light_value, t.dark_value, t.description
                        FROM design_tokens t
                        JOIN tokens_fts f ON t.id = f.rowid
                        WHERE tokens_fts MATCH $query
                        ORDER BY rank
                        LIMIT $limit
                        """);
                    cmd.Parameters.AddWithValue("$query", ftsQuery);
                    cmd.Parameters.AddWithValue("$limit", normalizedLimit);
                    var results = ReadTokens(cmd);
                    if (results.Count > 0) return results;
                }
                catch (SqliteException)
                {
                }
            }

            return SearchTokensLike(query, normalizedLimit);
        }
    }

    public List<DesignToken> GetTokensByCategory(string category)
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            using var cmd = CreateCommand("SELECT name, category, light_value, dark_value, description FROM design_tokens WHERE category = $cat ORDER BY name");
            cmd.Parameters.AddWithValue("$cat", category);
            return ReadTokens(cmd);
        }
    }

    public List<DesignToken> GetAllTokens()
    {
        lock (_gate)
        {
            if (_connection is null) return [];
            using var cmd = CreateCommand("SELECT name, category, light_value, dark_value, description FROM design_tokens ORDER BY category, name");
            return ReadTokens(cmd);
        }
    }

    private List<ComponentInfo> SearchComponentsLike(string query, int limit)
    {
        using var cmd = CreateCommand("""
            SELECT name, namespace, assembly, base_type, category, description, axaml_namespace, properties_json, style_classes_json, theme_resources_json
            FROM components
            WHERE {0}
            ORDER BY name
            LIMIT $limit
            """);
        cmd.CommandText = string.Format(cmd.CommandText, BuildLikePredicate(cmd, ["name", "namespace", "base_type", "category", "description"], query));
        cmd.Parameters.AddWithValue("$limit", limit);
        return ReadComponents(cmd);
    }

    private List<EnumInfo> SearchEnumsLike(string query, int limit)
    {
        using var cmd = CreateCommand("""
            SELECT name, namespace, description, values_json
            FROM enums
            WHERE {0}
            ORDER BY name
            LIMIT $limit
            """);
        cmd.CommandText = string.Format(cmd.CommandText, BuildLikePredicate(cmd, ["name", "namespace", "description"], query));
        cmd.Parameters.AddWithValue("$limit", limit);
        return ReadEnums(cmd);
    }

    private List<ExampleInfo> SearchExamplesLike(string query, int limit)
    {
        using var cmd = CreateCommand("""
            SELECT component_name, showcase_page, axaml_source, code_behind_source, view_model_source
            FROM examples
            WHERE {0}
            ORDER BY showcase_page, component_name
            LIMIT $limit
            """);
        cmd.CommandText = string.Format(cmd.CommandText, BuildLikePredicate(cmd, ["component_name", "showcase_page", "axaml_source"], query));
        cmd.Parameters.AddWithValue("$limit", limit);
        return ReadExamples(cmd);
    }

    private List<DesignToken> SearchTokensLike(string query, int limit)
    {
        using var cmd = CreateCommand("""
            SELECT name, category, light_value, dark_value, description
            FROM design_tokens
            WHERE {0}
            ORDER BY category, name
            LIMIT $limit
            """);
        cmd.CommandText = string.Format(cmd.CommandText, BuildLikePredicate(cmd, ["name", "category", "light_value", "dark_value", "description"], query));
        cmd.Parameters.AddWithValue("$limit", limit);
        return ReadTokens(cmd);
    }

    private List<ComponentInfo> ReadComponents(SqliteCommand cmd)
    {
        var list = new List<ComponentInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ComponentInfo
            {
                Name = reader.GetString(0),
                Namespace = reader.GetString(1),
                Assembly = reader.GetString(2),
                BaseType = reader.GetString(3),
                Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                AxamlNamespace = reader.IsDBNull(6) ? null : reader.GetString(6),
                Properties = JsonSerializer.Deserialize<List<PropertyInfo>>(reader.GetString(7)) ?? [],
                StyleClasses = JsonSerializer.Deserialize<List<string>>(reader.GetString(8)) ?? [],
                ThemeResources = JsonSerializer.Deserialize<List<string>>(reader.GetString(9)) ?? []
            });
        }
        return list;
    }

    private List<EnumInfo> ReadEnums(SqliteCommand cmd)
    {
        var list = new List<EnumInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new EnumInfo
            {
                Name = reader.GetString(0),
                Namespace = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Values = JsonSerializer.Deserialize<List<EnumValueInfo>>(reader.GetString(3)) ?? []
            });
        }
        return list;
    }

    private List<ExampleInfo> ReadExamples(SqliteCommand cmd)
    {
        var list = new List<ExampleInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ExampleInfo
            {
                ComponentName = reader.GetString(0),
                ShowcasePage = reader.GetString(1),
                AxamlSource = reader.IsDBNull(2) ? null : reader.GetString(2),
                CodeBehindSource = reader.IsDBNull(3) ? null : reader.GetString(3),
                ViewModelSource = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return list;
    }

    private List<DesignToken> ReadTokens(SqliteCommand cmd)
    {
        var list = new List<DesignToken>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new DesignToken
            {
                Name = reader.GetString(0),
                Category = reader.GetString(1),
                LightValue = reader.IsDBNull(2) ? null : reader.GetString(2),
                DarkValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return list;
    }

    private SqliteCommand CreateCommand(string sql, SqliteTransaction? transaction = null)
    {
        if (_connection is null) throw new InvalidOperationException("Store not initialized");
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction;
        return cmd;
    }

    private void ExecuteNonQuery(string sql, SqliteTransaction? transaction = null)
    {
        using var cmd = CreateCommand(sql, transaction);
        cmd.ExecuteNonQuery();
    }

    private static int NormalizeLimit(int limit, int max = 100)
    {
        return Math.Clamp(limit, 1, max);
    }

    private static string? BuildFtsQuery(string query)
    {
        var terms = ExtractSearchTerms(query);
        return terms.Length == 0 ? null : string.Join(" OR ", terms.Select(term => $"{term}*"));
    }

    private static string BuildLikePredicate(SqliteCommand cmd, string[] columns, string query)
    {
        var terms = ExtractSearchTerms(query);
        if (terms.Length == 0)
            return "0 = 1";

        var clauses = new List<string>();
        for (var termIndex = 0; termIndex < terms.Length; termIndex++)
        {
            var parameterName = $"$term{termIndex}";
            cmd.Parameters.AddWithValue(parameterName, $"%{EscapeLikePattern(terms[termIndex])}%");

            foreach (var column in columns)
                clauses.Add($"{column} LIKE {parameterName} ESCAPE '\\'");
        }

        return string.Join(" OR ", clauses);
    }

    private static string[] ExtractSearchTerms(string query)
    {
        return SearchTermRegex.Matches(query)
            .Select(match => match.Value)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static string EscapeLikePattern(string value)
    {
        return value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        lock (_gate)
            _connection?.Dispose();
    }
}

public sealed class CatalogVersion
{
    public long Id { get; set; }
    public string LibraryVersion { get; set; } = "";
    public string? SourceCommit { get; set; }
    public string GeneratedAt { get; set; } = "";
    public int ComponentCount { get; set; }

    public CatalogVersion Clone()
    {
        return new CatalogVersion
        {
            Id = Id,
            LibraryVersion = LibraryVersion,
            SourceCommit = SourceCommit,
            GeneratedAt = GeneratedAt,
            ComponentCount = ComponentCount
        };
    }
}

public sealed class CatalogData
{
    public string LibraryVersion { get; set; } = "";
    public string? SourceCommit { get; set; }
    public List<ComponentInfo> Components { get; set; } = [];
    public List<EnumInfo> Enums { get; set; } = [];
    public List<ExampleInfo> Examples { get; set; } = [];
    public List<DesignToken> DesignTokens { get; set; } = [];
}