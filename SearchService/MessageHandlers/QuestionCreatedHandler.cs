using System.Text.RegularExpressions;
using Contracts;
using SearchService.Models;
using Typesense;

namespace SearchService.MessageHandlers;

// Handler or Consumer are the same thing
// It is very important for Wolverine to have the suffix "Handler" or "Consumer" in order to work properly
public class QuestionCreatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionCreated message)
    {
        var created = new DateTimeOffset(message.CreatedAt).ToUnixTimeSeconds();

        var doc = new SearchQuestion
        {
            Id = message.QuestionId,
            CreatedAt = created,
            Title = message.Title,
            Content = StripHtml(message.Content),
            Tags = message.Tags.ToArray()
        };

        await client.CreateDocument("questions", doc);
        Console.WriteLine($"Created question with id {message.QuestionId}");
    }

    private static string StripHtml(string content)
    {
        return Regex.Replace(content, "<.*?>", string.Empty);
    }
}