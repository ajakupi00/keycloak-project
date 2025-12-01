namespace Contracts;

public record QuestionCreated(
    string QuestionId, 
    string Title, 
    string Content, 
    DateTime CreatedAt, 
    List<string> Tags);