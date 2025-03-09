namespace BATTARI_api.Models.DTO;

public class SummarizeConversationDto
{
    public SummarizeConversationDto(string sid, string keyword)
    {
        Sid = sid;
        Keyword = keyword;
    }

    public String Sid { get; set; }
    public String Keyword { get; set; }
}