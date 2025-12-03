using Contracts;
using Typesense;

namespace SearchService.MessageHandlers;

public class AnswerCountHandler(ITypesenseClient client)
{
    public async Task HandleAsync(UpdatedAnswerCount message)
    {
        await client.UpdateDocument("questions", message.questionId, new
        {
            message.answerCount
        });
    }
}