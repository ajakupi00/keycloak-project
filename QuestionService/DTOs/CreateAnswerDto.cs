using System.ComponentModel.DataAnnotations;
using QuestionService.Validators;

namespace QuestionService.DTOs;

public record CreateAnswerDto([Required] string Content);