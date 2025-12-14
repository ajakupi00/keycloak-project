using Common;

namespace QuestionService.RequestHelpers;

public record QuestionQuery : PaginationRequest
{
    public string? Tag { get; set; }
    public string? Sort { get; set; }
}