using LiteDB;
using MsgBox.Data.Models;

namespace MsgBox.Data.Migrations;

public sealed class SeedDemoDataMigration : IMigration
{
    public string Id => "20260330100000_SeedDemoData";

    public void Up(LiteDatabase db)
    {
        var peopleCol = db.GetCollection<Person>("people");
        if (peopleCol.Count() > 0)
            return;

        var now = DateTime.UtcNow;
        var john = new Person
        {
            Id = "person_" + Guid.NewGuid().ToString("n"),
            FirstName = "John",
            LastName = "Smith",
            DisplayName = "John Smith",
            ForeColor = "#ffffff",
            BackColor = "#0d6efd",
            CreatedUtc = now,
            UpdatedUtc = now
        };
        var jane = new Person
        {
            Id = "person_" + Guid.NewGuid().ToString("n"),
            FirstName = "Jane",
            LastName = "Doe",
            DisplayName = "Jane Doe",
            ForeColor = "#ffffff",
            BackColor = "#d63384",
            CreatedUtc = now,
            UpdatedUtc = now
        };

        peopleCol.Insert(john);
        peopleCol.Insert(jane);

        var chat = new Chat
        {
            Id = "chat_" + Guid.NewGuid().ToString("n"),
            ChatName = null,
            ChatType = "person",
            PersonIds = new List<string> { john.Id, jane.Id },
            CreatedUtc = now
        };
        db.GetCollection<Chat>("chats").Insert(chat);

        var msgCol = db.GetCollection<Message>("messages");
        var baseTime = now.AddHours(-2);
        msgCol.Insert(new Message
        {
            Id = "msg_" + Guid.NewGuid().ToString("n"),
            ChatId = chat.Id,
            AuthorPersonId = jane.Id,
            SentUtc = baseTime,
            Text = "Hello this is a teams message, about topics for work and other information about information that is business related because of business we have a job that is jobby.",
            Reactions = new List<MessageReaction>
            {
                new() { Emoji = "👍", PersonIds = new List<string> { john.Id } }
            }
        });
        msgCol.Insert(new Message
        {
            Id = "msg_" + Guid.NewGuid().ToString("n"),
            ChatId = chat.Id,
            AuthorPersonId = john.Id,
            SentUtc = baseTime.AddMinutes(12),
            Text = "Thanks for reaching out about business topics that are about business Jane. Let's have a meeting about these topics to do proper business stuff with this information we have.",
            Reactions = new List<MessageReaction>
            {
                new() { Emoji = "👍", PersonIds = new List<string> { jane.Id } }
            }
        });

        db.GetCollection<AppSettings>("app_settings").Insert(new AppSettings());
    }
}
