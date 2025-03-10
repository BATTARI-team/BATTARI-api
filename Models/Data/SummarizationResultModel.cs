using System.ComponentModel.DataAnnotations;

namespace BATTARI_api.Models.Data;

public class SummarizationResultModel
{
    public SummarizationResultModel(int callId, String sid)
    {
        CallId = callId;
        Sid = sid;
    }

    [Key]
    public String Sid { get; set; }
    // 申請者がここに来るようにする
    public int CallId { get; set; }
    public String? Keyword { get; set; }
}

public class SummarizationResultDto
{
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public String? Keyword { get; set; }
    public int CallId { get; set; }
}