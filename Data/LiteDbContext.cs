using LiteDB;
using MsgBox.Services;

namespace MsgBox.Data;

public class LiteDbContext
{
    private readonly string _connectionString;

    public LiteDbContext(AppStoragePaths paths)
    {
        _connectionString = $"Filename={paths.DatabasePath};Connection=shared";
    }

    public LiteDatabase Open() => new(_connectionString);
}
