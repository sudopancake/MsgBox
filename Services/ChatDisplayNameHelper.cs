using MsgBox.Data.Models;

namespace MsgBox.Services;

public static class ChatDisplayNameHelper
{
    public static string Resolve(Chat chat, Dictionary<string, Person> peopleById, string? currentUserPersonId = null)
    {
        if (!string.IsNullOrWhiteSpace(chat.ChatName))
            return chat.ChatName.Trim();

        if (chat.ChatType == "person")
        {
            var resolved = chat.PersonIds
                .Where(peopleById.ContainsKey)
                .Select(id => peopleById[id])
                .OrderBy(p => p.DisplayName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(currentUserPersonId))
            {
                var other = resolved.FirstOrDefault(p => p.Id != currentUserPersonId);
                if (other != null)
                    return other.DisplayName;
            }

            if (resolved.Count > 0)
                return resolved[0].DisplayName;
            return "Direct chat";
        }

        var names = chat.PersonIds
            .Select(id => peopleById.TryGetValue(id, out var p) ? p.DisplayName : id)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        if (names.Count == 0)
            return "Group chat";
        if (names.Count <= 3)
            return string.Join(", ", names);

        return $"{names[0]}, {names[1]} +{names.Count - 2}";
    }
}
