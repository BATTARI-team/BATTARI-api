using System.ComponentModel.DataAnnotations;

namespace BATTARI_api.Models.Data;

public class SummarizationResultModel
{
    [Key]
    public int Sid { get; set; }
    // 申請者がここに来るようにする
    public int CallId { get; set; }
    public String? Keyword { get; set; }
}