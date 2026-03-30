using LiteDB;
using MsgBox.Data.Models;

namespace MsgBox.Data.Repositories;

public class PersonRepository
{
    private readonly LiteDbContext _db;

    public PersonRepository(LiteDbContext db) => _db = db;

    public List<Person> GetAll()
    {
        using var database = _db.Open();
        return database.GetCollection<Person>("people").FindAll().OrderBy(p => p.DisplayName).ToList();
    }

    public Person? GetById(string id)
    {
        using var database = _db.Open();
        return database.GetCollection<Person>("people").FindById(id);
    }

    public Dictionary<string, Person> GetByIds(IEnumerable<string> ids)
    {
        var set = ids.Distinct().ToHashSet();
        using var database = _db.Open();
        var col = database.GetCollection<Person>("people");
        var map = new Dictionary<string, Person>();
        foreach (var id in set)
        {
            var p = col.FindById(id);
            if (p != null)
                map[id] = p;
        }
        return map;
    }

    /// <summary>
    /// Returns all people whose display name equals <paramref name="name"/> (trimmed, ordinal case-insensitive).
    /// Empty = unknown; multiple = ambiguous display name.
    /// </summary>
    public List<Person> FindByDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<Person>();

        var target = name.Trim();
        using var database = _db.Open();
        return database.GetCollection<Person>("people").FindAll()
            .Where(p => string.Equals(p.DisplayName.Trim(), target, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void Insert(Person person)
    {
        using var database = _db.Open();
        database.GetCollection<Person>("people").Insert(person);
    }

    public bool Update(Person person)
    {
        using var database = _db.Open();
        return database.GetCollection<Person>("people").Update(person);
    }
}
