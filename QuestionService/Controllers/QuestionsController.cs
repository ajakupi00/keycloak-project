using System.Security.Claims;
using Common;
using Contracts;
using Ganss.Xss;
using JasperFx.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using QuestionService.RequestHelpers;
using QuestionService.Services;
using Reputation;
using Wolverine;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext db, IMessageBus bus, TagService tagService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto dto)
    {
        if (!await tagService.AreTagsValidAsync(dto.Tags))
            return BadRequest("Invalid tags");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null) return BadRequest("Cannot get user details");

        var sanitizer = new HtmlSanitizer();

        var question = new Question
        {
            Title = dto.Title,
            Content = sanitizer.Sanitize(dto.Content),
            TagSlugs = dto.Tags,
            AskerId = userId,
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        
        var slugs = question.TagSlugs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (slugs.Length > 0)
        {
            await db.Tags
                .Where(t => slugs.Contains(t.Slug))
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.UsageCount, t => t.UsageCount + 1));
        }
        
        
        // Publish message to the RabbitMQ bus
        await bus.PublishAsync(new QuestionCreated(
            question.Id, question.Title, 
            question.Content, question.CreatedAt ,question.TagSlugs));

        return Created($"questions/{question.Id}", question);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginationResult<Question>>> GetQuestions([FromQuery] QuestionQuery q)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(q.Tag))
            query = query.Where(x => x.TagSlugs.Contains(q.Tag));

        query = query.OrderByDescending(x => x.CreatedAt);

        query = q.Sort switch
        {
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            "active" => query.OrderByDescending(x => new[]
            {
                x.CreatedAt,
                x.UpdatedAt ?? DateTime.MinValue,
                x.Answers.Max(a => (DateTime?)a.CreatedAt) ?? DateTime.MinValue,
                x.Answers.Max(a => a.UpdatedAt) ?? DateTime.MinValue,
            }.Max()),
            "unanswered" => query.Where(x => x.AnswerCount == 0).OrderByDescending(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var result = await query.ToPaginationResult(q);

        return result;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await db.Questions
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id);
        
        if (question is null) return NotFound();

        await db.Questions
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));
        
        return question;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto dto)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();
        
        if (!await tagService.AreTagsValidAsync(dto.Tags))
            return BadRequest("Invalid tags");

        var original = question.TagSlugs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var incoming = dto.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        
        var removed = original.Except(incoming, StringComparer.OrdinalIgnoreCase).ToArray();
        var added = incoming.Except(original, StringComparer.OrdinalIgnoreCase).ToArray();
        
        var sanitizer = new HtmlSanitizer();
        
        question.Title = dto.Title;
        question.Content = sanitizer.Sanitize(dto.Content);
        question.TagSlugs = dto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (removed.Length > 0)
        {
            await db.Tags
                .Where(t => removed.Contains(t.Slug) && t.UsageCount > 0)
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.UsageCount, t => t.UsageCount - 1));
        }
        
        if (added.Length > 0)
        {
            await db.Tags
                .Where(t => added.Contains(t.Slug))
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.UsageCount, t => t.UsageCount + 1));
        }
        
        await bus.PublishAsync(new QuestionUpdated(
            question.Id, question.Title, 
            question.Content, question.TagSlugs.ToArray()));
        
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();
        
        db.Questions.Remove(question);
        await db.SaveChangesAsync();

        await bus.PublishAsync(new QuestionDeleted(question.Id));
        
        return NoContent();
    }

    [Authorize]
    [HttpPost("{questionId}/answers")]
    public async Task<ActionResult> CreateAnswer(string questionId, CreateAnswerDto dto)
    {
        var question = await db.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if (userId is null || name is null) return BadRequest("Cannot get user details");
        
        var sanitizer = new HtmlSanitizer();
        
        var answer = new Answer
        {
            Content =  sanitizer.Sanitize(dto.Content),
            QuestionId = questionId,
            UserId =  userId,
        };
        
        question.AnswerCount++;
        question.Answers.Add(answer);
        
        await db.SaveChangesAsync();
        
        await bus.PublishAsync(new UpdatedAnswerCount(question.Id, question.AnswerCount));

        return Created($"questions/{question.Id}", answer);
    }

    [Authorize]
    [HttpPut("{questionId}/answers/{answerId}")]
    public async Task<ActionResult> UpdateAnswer(string questionId, string answerId, CreateAnswerDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if (userId is null || name is null) return BadRequest("Cannot get user details");
        
        var answer = await db.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        
        if (userId != answer.UserId) return BadRequest("You are not authorized to update others answers.");
        
        var sanitizer = new HtmlSanitizer();
        
        answer.Content = sanitizer.Sanitize(dto.Content);
        answer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{questionId}/answers/{answerId}")]
    public async Task<ActionResult> DeleteAnswer(string questionId, string answerId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if (userId is null || name is null) return BadRequest("Cannot get user details");
        
        var answer = await db.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        
        var question = await db.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        if (userId != answer.UserId) return BadRequest("You are not authorized to delete others answers.");
        
        question.AnswerCount--;
        db.Answers.Remove(answer);
        
        await  db.SaveChangesAsync();
        
        await bus.PublishAsync(new UpdatedAnswerCount(question.Id, question.AnswerCount));
        
        return NoContent();
    }

    [Authorize]
    [HttpPost("{questionId}/answers/{answerId}/accept")]
    public async Task<ActionResult> AcceptAnswer(string questionId, string answerId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if (userId is null || name is null) return BadRequest("Cannot get user details");
        
        var answer = await db.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        
        var question = await db.Questions.FindAsync(questionId);
        if (question is null) return NotFound();
        
        if (userId != question.AskerId) return BadRequest("You are not authorized to accept answers.");
        
        if (question.HasAcceptedAnswer) return BadRequest("Answer has been already accepted.");

        question.HasAcceptedAnswer = true;
        answer.Accepted = true;
        
        await  db.SaveChangesAsync();

        await bus.PublishAsync(new AnswerAccepted(question.Id));
        await bus.PublishAsync(ReputationHelper.MakeEvent(
            answer.UserId, 
            ReputationReason.AnswerAccepted,
            question.AskerId));
        
        return NoContent();
    }


    
}