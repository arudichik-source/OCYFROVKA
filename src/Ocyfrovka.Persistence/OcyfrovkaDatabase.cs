using Microsoft.Data.Sqlite;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;
using System.Globalization;
using System.Text.Json;

namespace Ocyfrovka.Persistence;

public sealed class OcyfrovkaDatabase
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.General);

    private readonly string _databasePath;
    private readonly string _connectionString;

    public OcyfrovkaDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException(
                "Database path is required.",
                nameof(databasePath));
        }

        _databasePath = Path.GetFullPath(databasePath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        };

        _connectionString = builder.ToString();
    }

    public string DatabasePath => _databasePath;

    public static string GetDefaultPath(string applicationBaseDirectory)
        => Path.Combine(
            applicationBaseDirectory,
            "Data",
            "digitizer.db");

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();

        var version = ReadSchemaVersion(connection);

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Database schema {version} is newer than supported schema {CurrentSchemaVersion}.");
        }

        if (version == 0)
        {
            CreateSchemaV1(connection);
            SetSchemaVersion(connection, 1);
        }
    }

    public void SavePageReview(PersistedPageReview page)
    {
        ArgumentNullException.ThrowIfNull(page);

        Initialize();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var reviewState = page.Review is null
            ? ReviewSessionState.New
            : ReviewTableSession
                .FromSnapshot(page.Review)
                .GetSummary()
                .State;

        using (var documentCommand = connection.CreateCommand())
        {
            documentCommand.Transaction = transaction;
            documentCommand.CommandText =
                """
                INSERT INTO Documents(
                    DocumentId,
                    DisplayName,
                    ReviewState,
                    UpdatedUtc)
                VALUES(
                    $documentId,
                    $displayName,
                    $reviewState,
                    $updatedUtc)
                ON CONFLICT(DocumentId) DO UPDATE SET
                    DisplayName = excluded.DisplayName,
                    ReviewState = excluded.ReviewState,
                    UpdatedUtc = excluded.UpdatedUtc;
                """;

            documentCommand.Parameters.AddWithValue(
                "$documentId",
                page.DocumentId.ToString("D"));

            documentCommand.Parameters.AddWithValue(
                "$displayName",
                Path.GetFileName(page.SourcePath));

            documentCommand.Parameters.AddWithValue(
                "$reviewState",
                reviewState.ToString());

            documentCommand.Parameters.AddWithValue(
                "$updatedUtc",
                FormatDate(page.UpdatedUtc));

            documentCommand.ExecuteNonQuery();
        }

        using (var pageCommand = connection.CreateCommand())
        {
            pageCommand.Transaction = transaction;
            pageCommand.CommandText =
                """
                INSERT INTO Pages(
                    PageId,
                    DocumentId,
                    SourcePath,
                    SourceFrameIndex,
                    RotationDegrees,
                    OcrText,
                    ColumnCount,
                    HeaderRowIndex,
                    ReviewState,
                    UpdatedUtc)
                VALUES(
                    $pageId,
                    $documentId,
                    $sourcePath,
                    $sourceFrameIndex,
                    $rotationDegrees,
                    $ocrText,
                    $columnCount,
                    $headerRowIndex,
                    $reviewState,
                    $updatedUtc)
                ON CONFLICT(PageId) DO UPDATE SET
                    DocumentId = excluded.DocumentId,
                    SourcePath = excluded.SourcePath,
                    SourceFrameIndex = excluded.SourceFrameIndex,
                    RotationDegrees = excluded.RotationDegrees,
                    OcrText = excluded.OcrText,
                    ColumnCount = excluded.ColumnCount,
                    HeaderRowIndex = excluded.HeaderRowIndex,
                    ReviewState = excluded.ReviewState,
                    UpdatedUtc = excluded.UpdatedUtc;
                """;

            pageCommand.Parameters.AddWithValue(
                "$pageId",
                page.PageId.ToString("D"));

            pageCommand.Parameters.AddWithValue(
                "$documentId",
                page.DocumentId.ToString("D"));

            pageCommand.Parameters.AddWithValue(
                "$sourcePath",
                page.SourcePath ?? string.Empty);

            pageCommand.Parameters.AddWithValue(
                "$sourceFrameIndex",
                page.SourceFrameIndex);

            pageCommand.Parameters.AddWithValue(
                "$rotationDegrees",
                page.RotationDegrees);

            pageCommand.Parameters.AddWithValue(
                "$ocrText",
                page.OcrText ?? string.Empty);

            pageCommand.Parameters.AddWithValue(
                "$columnCount",
                page.Review?.ColumnCount ?? 0);

            var headerParameter =
                pageCommand.Parameters.Add(
                    "$headerRowIndex",
                    SqliteType.Integer);

            headerParameter.Value =
                page.Review?.HeaderRowIndex is int headerIndex
                    ? headerIndex
                    : DBNull.Value;

            pageCommand.Parameters.AddWithValue(
                "$reviewState",
                reviewState.ToString());

            pageCommand.Parameters.AddWithValue(
                "$updatedUtc",
                FormatDate(page.UpdatedUtc));

            pageCommand.ExecuteNonQuery();
        }

        using (var deleteCells = connection.CreateCommand())
        {
            deleteCells.Transaction = transaction;
            deleteCells.CommandText =
                "DELETE FROM ReviewCells WHERE PageId = $pageId;";

            deleteCells.Parameters.AddWithValue(
                "$pageId",
                page.PageId.ToString("D"));

            deleteCells.ExecuteNonQuery();
        }

        if (page.Review is not null)
        {
            foreach (var cell in page.Review.Cells)
            {
                InsertReviewCell(
                    connection,
                    transaction,
                    page.PageId,
                    cell,
                    page.UpdatedUtc);
            }
        }

        transaction.Commit();
    }

    public PersistedPageReview? LoadPageReview(Guid pageId)
    {
        Initialize();

        using var connection = OpenConnection();

        Guid documentId;
        string sourcePath;
        int sourceFrameIndex;
        int rotationDegrees;
        string ocrText;
        int columnCount;
        int? headerRowIndex;
        DateTimeOffset updatedUtc;

        using (var pageCommand = connection.CreateCommand())
        {
            pageCommand.CommandText =
                """
                SELECT
                    DocumentId,
                    SourcePath,
                    SourceFrameIndex,
                    RotationDegrees,
                    OcrText,
                    ColumnCount,
                    HeaderRowIndex,
                    UpdatedUtc
                FROM Pages
                WHERE PageId = $pageId;
                """;

            pageCommand.Parameters.AddWithValue(
                "$pageId",
                pageId.ToString("D"));

            using var reader = pageCommand.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            documentId = Guid.Parse(reader.GetString(0));
            sourcePath = reader.GetString(1);
            sourceFrameIndex = reader.GetInt32(2);
            rotationDegrees = reader.GetInt32(3);
            ocrText = reader.GetString(4);
            columnCount = reader.GetInt32(5);
            headerRowIndex = reader.IsDBNull(6)
                ? null
                : reader.GetInt32(6);
            updatedUtc = ParseDate(reader.GetString(7));
        }

        var cells = new List<ReviewCellSnapshot>();

        using (var cellsCommand = connection.CreateCommand())
        {
            cellsCommand.CommandText =
                """
                SELECT
                    RowIndex,
                    ColumnIndex,
                    SourceText,
                    CurrentText,
                    SuggestedText,
                    Confidence,
                    BoundsX,
                    BoundsY,
                    BoundsWidth,
                    BoundsHeight,
                    SourceWordsJson,
                    State
                FROM ReviewCells
                WHERE PageId = $pageId
                ORDER BY RowIndex, ColumnIndex;
                """;

            cellsCommand.Parameters.AddWithValue(
                "$pageId",
                pageId.ToString("D"));

            using var reader = cellsCommand.ExecuteReader();

            while (reader.Read())
            {
                var wordsJson = reader.GetString(10);
                var words = JsonSerializer.Deserialize<OcrWord[]>(
                    wordsJson,
                    JsonOptions) ?? [];

                var stateText = reader.GetString(11);
                if (!Enum.TryParse<ReviewCellState>(
                        stateText,
                        ignoreCase: false,
                        out var state))
                {
                    state = ReviewCellState.Error;
                }

                cells.Add(
                    new ReviewCellSnapshot(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.IsDBNull(4)
                            ? null
                            : reader.GetString(4),
                        reader.GetDouble(5),
                        new LayoutBounds(
                            reader.GetInt32(6),
                            reader.GetInt32(7),
                            reader.GetInt32(8),
                            reader.GetInt32(9)),
                        words,
                        state));
            }
        }

        ReviewTableSnapshot? review =
            cells.Count == 0 && columnCount == 0
                ? null
                : new ReviewTableSnapshot(
                    columnCount,
                    headerRowIndex,
                    cells);

        return new PersistedPageReview(
            documentId,
            pageId,
            sourcePath,
            sourceFrameIndex,
            rotationDegrees,
            ocrText,
            review,
            updatedUtc);
    }

    public IReadOnlyList<PersistedPageReview> LoadDocumentPages(
        Guid documentId)
    {
        Initialize();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT PageId
            FROM Pages
            WHERE DocumentId = $documentId
            ORDER BY SourceFrameIndex, PageId;
            """;

        command.Parameters.AddWithValue(
            "$documentId",
            documentId.ToString("D"));

        var ids = new List<Guid>();

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                ids.Add(Guid.Parse(reader.GetString(0)));
            }
        }

        return ids
            .Select(LoadPageReview)
            .Where(page => page is not null)
            .Cast<PersistedPageReview>()
            .ToArray();
    }

    public void RecordCorrection(
        string inputNormalized,
        string canonical,
        string fieldKind,
        DateTimeOffset? usedUtc = null)
    {
        if (string.IsNullOrWhiteSpace(inputNormalized))
        {
            throw new ArgumentException(
                "Normalized input is required.",
                nameof(inputNormalized));
        }

        if (string.IsNullOrWhiteSpace(canonical))
        {
            throw new ArgumentException(
                "Canonical value is required.",
                nameof(canonical));
        }

        if (string.IsNullOrWhiteSpace(fieldKind))
        {
            throw new ArgumentException(
                "Field kind is required.",
                nameof(fieldKind));
        }

        Initialize();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO CorrectionMemory(
                InputNormalized,
                FieldKind,
                Canonical,
                HitCount,
                LastUsedUtc)
            VALUES(
                $input,
                $fieldKind,
                $canonical,
                1,
                $lastUsedUtc)
            ON CONFLICT(InputNormalized, FieldKind, Canonical) DO UPDATE SET
                HitCount = HitCount + 1,
                LastUsedUtc = excluded.LastUsedUtc;
            """;

        command.Parameters.AddWithValue(
            "$input",
            inputNormalized.Trim());

        command.Parameters.AddWithValue(
            "$fieldKind",
            fieldKind.Trim());

        command.Parameters.AddWithValue(
            "$canonical",
            canonical.Trim());

        command.Parameters.AddWithValue(
            "$lastUsedUtc",
            FormatDate(usedUtc ?? DateTimeOffset.UtcNow));

        command.ExecuteNonQuery();
    }

    public IReadOnlyList<CorrectionMemoryEntry> FindCorrections(
        string inputNormalized,
        string fieldKind,
        int limit = 5)
    {
        if (limit <= 0 ||
            string.IsNullOrWhiteSpace(inputNormalized) ||
            string.IsNullOrWhiteSpace(fieldKind))
        {
            return [];
        }

        Initialize();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                InputNormalized,
                Canonical,
                FieldKind,
                HitCount,
                LastUsedUtc
            FROM CorrectionMemory
            WHERE
                InputNormalized = $input AND
                FieldKind = $fieldKind
            ORDER BY HitCount DESC, LastUsedUtc DESC, Canonical
            LIMIT $limit;
            """;

        command.Parameters.AddWithValue(
            "$input",
            inputNormalized.Trim());

        command.Parameters.AddWithValue(
            "$fieldKind",
            fieldKind.Trim());

        command.Parameters.AddWithValue(
            "$limit",
            limit);

        var results = new List<CorrectionMemoryEntry>();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(
                new CorrectionMemoryEntry(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    ParseDate(reader.GetString(4))));
        }

        return results;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText =
            """
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            """;
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void SetSchemaVersion(
        SqliteConnection connection,
        int version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }

    private static void CreateSchemaV1(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Documents(
                DocumentId TEXT PRIMARY KEY NOT NULL,
                DisplayName TEXT NOT NULL,
                ReviewState TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Pages(
                PageId TEXT PRIMARY KEY NOT NULL,
                DocumentId TEXT NOT NULL,
                SourcePath TEXT NOT NULL,
                SourceFrameIndex INTEGER NOT NULL,
                RotationDegrees INTEGER NOT NULL,
                OcrText TEXT NOT NULL DEFAULT '',
                ColumnCount INTEGER NOT NULL DEFAULT 0,
                HeaderRowIndex INTEGER NULL,
                ReviewState TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                FOREIGN KEY(DocumentId)
                    REFERENCES Documents(DocumentId)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Pages_DocumentId
                ON Pages(DocumentId);

            CREATE TABLE IF NOT EXISTS ReviewCells(
                PageId TEXT NOT NULL,
                RowIndex INTEGER NOT NULL,
                ColumnIndex INTEGER NOT NULL,
                SourceText TEXT NOT NULL,
                CurrentText TEXT NOT NULL,
                SuggestedText TEXT NULL,
                Confidence REAL NOT NULL,
                BoundsX INTEGER NOT NULL,
                BoundsY INTEGER NOT NULL,
                BoundsWidth INTEGER NOT NULL,
                BoundsHeight INTEGER NOT NULL,
                SourceWordsJson TEXT NOT NULL,
                State TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                PRIMARY KEY(PageId, RowIndex, ColumnIndex),
                FOREIGN KEY(PageId)
                    REFERENCES Pages(PageId)
                    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS CorrectionMemory(
                InputNormalized TEXT NOT NULL,
                FieldKind TEXT NOT NULL,
                Canonical TEXT NOT NULL,
                HitCount INTEGER NOT NULL DEFAULT 1,
                LastUsedUtc TEXT NOT NULL,
                PRIMARY KEY(InputNormalized, FieldKind, Canonical)
            );

            CREATE INDEX IF NOT EXISTS IX_CorrectionMemory_Lookup
                ON CorrectionMemory(
                    InputNormalized,
                    FieldKind,
                    HitCount DESC);
            """;

        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void InsertReviewCell(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid pageId,
        ReviewCellSnapshot cell,
        DateTimeOffset updatedUtc)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO ReviewCells(
                PageId,
                RowIndex,
                ColumnIndex,
                SourceText,
                CurrentText,
                SuggestedText,
                Confidence,
                BoundsX,
                BoundsY,
                BoundsWidth,
                BoundsHeight,
                SourceWordsJson,
                State,
                UpdatedUtc)
            VALUES(
                $pageId,
                $rowIndex,
                $columnIndex,
                $sourceText,
                $currentText,
                $suggestedText,
                $confidence,
                $boundsX,
                $boundsY,
                $boundsWidth,
                $boundsHeight,
                $sourceWordsJson,
                $state,
                $updatedUtc);
            """;

        command.Parameters.AddWithValue(
            "$pageId",
            pageId.ToString("D"));

        command.Parameters.AddWithValue(
            "$rowIndex",
            cell.RowIndex);

        command.Parameters.AddWithValue(
            "$columnIndex",
            cell.ColumnIndex);

        command.Parameters.AddWithValue(
            "$sourceText",
            cell.SourceText ?? string.Empty);

        command.Parameters.AddWithValue(
            "$currentText",
            cell.CurrentText ?? string.Empty);

        var suggestionParameter =
            command.Parameters.Add(
                "$suggestedText",
                SqliteType.Text);

        suggestionParameter.Value =
            cell.SuggestedText is null
                ? DBNull.Value
                : cell.SuggestedText;

        command.Parameters.AddWithValue(
            "$confidence",
            cell.Confidence);

        command.Parameters.AddWithValue(
            "$boundsX",
            cell.Bounds.X);

        command.Parameters.AddWithValue(
            "$boundsY",
            cell.Bounds.Y);

        command.Parameters.AddWithValue(
            "$boundsWidth",
            cell.Bounds.Width);

        command.Parameters.AddWithValue(
            "$boundsHeight",
            cell.Bounds.Height);

        command.Parameters.AddWithValue(
            "$sourceWordsJson",
            JsonSerializer.Serialize(
                cell.SourceWords ?? [],
                JsonOptions));

        command.Parameters.AddWithValue(
            "$state",
            cell.State.ToString());

        command.Parameters.AddWithValue(
            "$updatedUtc",
            FormatDate(updatedUtc));

        command.ExecuteNonQuery();
    }

    private static string FormatDate(DateTimeOffset value)
        => value.ToUniversalTime().ToString(
            "O",
            CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDate(string value)
        => DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
}
