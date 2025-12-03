using Contracts;
using Typesense;

namespace SearchService.MessageHandlers;

public class AcceptAnswerHandler(ITypesenseClient client)
{
    public async Task HandleAsync(AnswerAccepted message)
    {
        await client.UpdateDocument("questions", message.questionId, new
        {
            HasAcceptedAnswer = true
        });
    }
}