using BATTARI_api.Models.Data;
using BATTARI_api.Models.DTO;
using BATTARI_api.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BATTARI_api.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[AllowAnonymous]
public class SummarizeConversationController(ISummarizeConversationDatabase summarizeConversationDatabase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PutSummarizationResult(SummarizeConversationDto dto)
    
    {  
        await summarizeConversationDatabase.PutSummarizationResult(dto);
        return Ok(dto.Keyword);
    }

    [HttpGet]
    public async Task<IEnumerable<SummarizationResultDto>> GetSummarizations(int userId)
    {
        return await summarizeConversationDatabase.GetSummarizationResult(userId);
    }
    
    [HttpPost]
    public async Task<IActionResult> SetSid(String sid, int callId)
    {
        await summarizeConversationDatabase.SetSid(sid, callId);
        return Ok();
    }
}
