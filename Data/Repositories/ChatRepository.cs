using LiteDB;
using MsgBox.Data.Models;

namespace MsgBox.Data.Repositories;

public class ChatRepository
{
    private readonly LiteDbContext _db;

    public ChatRepository(LiteDbContext db) => _db = db;

    public List<Chat> GetAll()
    {
        using var database = _db.Open();
        return database.GetCollection<Chat>("chats").FindAll().OrderByDescending(c => c.CreatedUtc).ToList();
    }

    public Chat? GetById(string id)
    {
        using var database = _db.Open();
        return database.GetCollection<Chat>("chats").FindById(id);
    }

    public void Insert(Chat chat)
    {
        using var database = _db.Open();
        database.GetCollection<Chat>("chats").Insert(chat);
    }
}
