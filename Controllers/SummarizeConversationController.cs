using BATTARI_api.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BATTARI_api.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class SummarizeConversationController : ControllerBase
{
    [HttpPost]
    public IActionResult PutSummarizationResult(SummarizeConversationDto dto)
    {
        return Ok(dto.Keyword);
    }
}
