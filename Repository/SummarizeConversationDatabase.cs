using BATTARI_api.Models.Data;
using BATTARI_api.Models.DTO;
using BATTARI_api.Repository.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BATTARI_api.Repository;

public interface ISummarizeConversationDatabase
{
    public Task PutSummarizationResult(SummarizeConversationDto dto);
    public Task SetSid(String sid, int callId);
    public Task<IEnumerable<SummarizationResultDto>> GetSummarizationResult(int userIndex);
}

public class SummarizeConversationDatabase(UserContext context, ICallRepository _callRepository) : ISummarizeConversationDatabase
{
    public async Task PutSummarizationResult(SummarizeConversationDto dto)
    {
            try
            {
                var summarizationModel = await context.SummarizationResults.FindAsync(dto.Sid);
                if (summarizationModel == null)
                    throw new KeyNotFoundException("SummarizationResultModel not found");
                summarizationModel.Keyword = dto.Keyword;
                context.Update(summarizationModel);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
    }

    public async Task SetSid(String sid, int callId)
    {
        // #TODO callがなかった時
        try
        {
            await context.AddAsync(new SummarizationResultModel(callId: callId, sid: sid));
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }   
    }

    public async Task<IEnumerable<SummarizationResultDto>> GetSummarizationResult(int userIndex)
    {
        var query = from summarize in context.Set<SummarizationResultModel>()
            join users in context.Set<CallModel>()
                on summarize.CallId equals users.CallId
            where users.User1Id == userIndex || users.User2Id == userIndex
            select new
            {
                users.User1Id,
                users.User2Id,
                summarize.Keyword,
                summarize.CallId
            };
        var result = query.Take(10).AsEnumerable();
        var list = new List<SummarizationResultDto>();
        foreach (var r in result)
        {
            list.Add(new SummarizationResultDto()
            {
                User1Id = r.User1Id,
                User2Id = r.User2Id,
                Keyword = r.Keyword,
                CallId = r.CallId
            });
        }

        return list;
    }
}