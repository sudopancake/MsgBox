using LiteDB;
using MsgBox.Data.Models;

namespace MsgBox.Data.Migrations;

public class MigrationRunner
{
    public const string AppliedMigrationsCollection = "schema_migrations";

    private readonly IReadOnlyList<IMigration> _migrations;

    public MigrationRunner(IEnumerable<IMigration> migrations)
    {
        _migrations = migrations.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
    }

    public void ApplyPending(LiteDatabase db)
    {
        var appliedCol = db.GetCollection<AppliedMigrationRecord>(AppliedMigrationsCollection);
        var appliedIds = new HashSet<string>(
            appliedCol.FindAll().Select(r => r.Id),
            StringComparer.Ordinal);

        foreach (var migration in _migrations)
        {
            if (appliedIds.Contains(migration.Id))
                continue;

            migration.Up(db);

            appliedCol.Insert(new AppliedMigrationRecord
            {
                Id = migration.Id,
                AppliedUtc = DateTime.UtcNow
            });
            appliedIds.Add(migration.Id);
        }
    }
}
