using LiteDB;

namespace MsgBox.Data.Migrations;

public interface IMigration
{
    string Id { get; }
    void Up(LiteDatabase db);
}
