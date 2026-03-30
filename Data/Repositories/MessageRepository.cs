using LiteDB;
using MsgBox.Data.Models;

namespace MsgBox.Data.Repositories;

public class MessageRepository
{
    private readonly LiteDbContext _db;

    public MessageRepository(LiteDbContext db) => _db = db;

    public Message? GetById(string id)
    {
        using var database = _db.Open();
        return database.GetCollection<Message>("messages").FindById(id);
    }

    /// <summary>
    /// Returns up to <paramref name="take"/> newest messages in the chat, then ordered oldest → newest.
    /// </summary>
    public List<Message> GetLatestPage(string chatId, int take, DateTime? olderThanSentUtc)
    {
        using var database = _db.Open();
        var col = database.GetCollection<Message>("messages");
        var query = col.Query().Where(m => m.ChatId == chatId);
        if (olderThanSentUtc.HasValue)
            query = query.Where(m => m.SentUtc < olderThanSentUtc.Value);

        var batch = query.OrderByDescending(m => m.SentUtc).Limit(take).ToList();
        batch.Reverse();
        return batch;
    }

    public (string? Text, DateTime? SentUtc) GetLatestForChat(string chatId)
    {
        using var database = _db.Open();
        var col = database.GetCollection<Message>("messages");
        var latest = col.Query().Where(m => m.ChatId == chatId).OrderByDescending(m => m.SentUtc).FirstOrDefault();
        if (latest == null)
            return (null, null);
        var preview = latest.Text.Length > 120 ? latest.Text[..120] + "…" : latest.Text;
        return (preview, latest.SentUtc);
    }

    public void Insert(Message message)
    {
        using var database = _db.Open();
        database.GetCollection<Message>("messages").Insert(message);
    }

    public void InsertMany(IEnumerable<Message> messages)
    {
        using var database = _db.Open();
        var col = database.GetCollection<Message>("messages");
        col.InsertBulk(messages);
    }

    public bool Update(Message message)
    {
        using var database = _db.Open();
        return database.GetCollection<Message>("messages").Update(message);
    }
}
