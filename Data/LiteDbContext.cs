using LiteDB;
using Microsoft.Extensions.Options;

namespace MsgBox.Data;

public class LiteDbContext
{
    private readonly string _connectionString;

    public LiteDbContext(IOptions<LiteDbOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public LiteDatabase Open() => new(_connectionString);
}
