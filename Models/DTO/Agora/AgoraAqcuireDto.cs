using System.Text.Json.Serialization;

namespace BATTARI_api.Models.DTO.Agora;

public class AgoraAqcuireDto
{
    // {
    //     "cname": "string",
    //     "uid": "string",
    //     "resourceId": "string"
    // }
    [JsonPropertyName("cname")]
    public String Cname { get; set; }
    [JsonPropertyName("uid")]
    public String Uid { get; set; }
    [JsonPropertyName("resourceId")]
    public String ResourceId { get; set; }
}