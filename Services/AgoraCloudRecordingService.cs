using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgoraIO.Media;
using BATTARI_api.Models.DTO.Agora;
using BATTARI_api.Repository;

namespace BATTARI_api.Services;

public class AgoraCloudRecordingService(IConfiguration _configuration, ISummarizeConversationDatabase summarizeConversationDatabase)
{
    const String uid = "1000";
    public async Task<String> GetResource(String channel)
    {
        HttpClient client = new HttpClient();
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                cname = channel,
                uid = uid,
                clientRequest = new
                {
                    resourceExpiredHour = 24,
                    scene = 0
                }
            }),
            Encoding.UTF8,
            "application/json");
        
        var appid = _configuration["Agora:AppId"];
        var apiKey = _configuration["Agora:AgoraRestApiKey"];
        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.agora.io/v1/apps/{appid}/cloud_recording/acquire"); //Replace "YOUR_ENDPOINT_HERE"
        request.Content = jsonContent;
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", apiKey);

        HttpResponseMessage response = await client.SendAsync(request);
        
        var todo = await response.Content.ReadFromJsonAsync<AgoraAqcuireDto>();
        Console.WriteLine("Resource acquired with resourceId: " + todo.ResourceId);
        return todo.ResourceId;
    }
    
    public async Task SetSid(String sid, int callId)
    {
        Console.WriteLine("Setting sid: " + sid + ", callId:" + callId);
        await summarizeConversationDatabase.SetSid(sid, callId);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="channel"></param>
    /// <returns>sid</returns>
    public async Task<String> StartRecording(String resourceId, String channel)
    {
        HttpClient client = new HttpClient();
        
        var appid = _configuration["Agora:AppId"];
        var apiKey = _configuration["Agora:AgoraRestApiKey"];
        
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                cname = channel,
                uid = uid,
                clientRequest = new
                {
                    token = _generateToken(int.Parse(uid), channel),
                    recordingConfig = new
                    {
                        maxIdleTime = 1,
                        streamTypes = 0
                    },
                    recordingFileConfig = new
                    {
                        avFileType = new[] {"hls", "mp4"}
                    },
                    storageConfig = new
                    {
                        vendor = 6,
                        region = 0,
                        bucket = _configuration["Gcs:Bucket"],
                        accessKey = _configuration["Gcs:AccessKey"],
                        secretKey = _configuration["Gcs:SecretKey"],
                    },
                }
            }),
            Encoding.UTF8,
            "application/json");
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.agora.io/v1/apps/{appid}/cloud_recording/resourceid/{resourceId}/mode/mix/start"); //Replace "YOUR_ENDPOINT_HERE"
        request.Content = jsonContent;
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", apiKey);

        HttpResponseMessage response = await client.SendAsync(request);
        
        var recordingDto = await response.Content.ReadFromJsonAsync<AgoraStartRecordingDto>();
        Console.WriteLine("Recording started with sid: " + recordingDto.sid);
        return recordingDto.sid;
    }
    
    private string _generateToken(int uid, string channelId)
    {
        AccessToken accessToken = new AccessToken(_configuration["Agora:AppID"], _configuration["Agora:AppCertificate"], channelId, uid.ToString());
        var result = accessToken.Build();
        if (result == null) throw new Exception("Token build failed");
        return result;
    }

}