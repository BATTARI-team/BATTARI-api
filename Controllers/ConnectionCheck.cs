using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AgoraIO.Media;
using BATTARI_api.Models.DTO;
using BATTARI_api.Repository;
using BATTARI_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BATTARI_api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class DeveloperController
(IConfiguration configuration, UserOnlineConcurrentDictionaryDatabase userOnlineConcurrentDictionaryDatabase, CallingService callingService, ISouguuService souguuService) : ControllerBase
{
    /// <summary>
    /// ログインしてないと使えません
    /// </summary>
    /// <returns></returns> <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize]
    public IActionResult ConnectionCheck()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var claim = identity?.Claims.FirstOrDefault(c => c.Type == "name");

        if (claim != null)
        {
            Console.WriteLine(claim.Value);
        }
        Console.WriteLine(configuration["Pepper"]);

        return Ok("Connection is working. Welcome " + claim?.Value + "!");
    }

    [HttpGet]
    public IActionResult GetAgoraToken(String channelId, int uid)
    {

        AccessToken accessToken =
            new AccessToken(configuration["Agora:AppId"] ?? throw new ArgumentNullException("AppIdがappsettings.jsonに設定されていません。,"), configuration["Agora:AppCertificate"] ?? throw new ArgumentNullException("AppCertificateがappsettings.jsonに設定されていません。"),
                            channelId, uid.ToString());
        string result = accessToken.Build();
        return Ok(result);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [HttpPut]
    public IActionResult JwtParse(String input)
    {
        var jsonToken = new JwtSecurityTokenHandler().ReadToken(input);

        return Ok(jsonToken);
    }

    /// <summary>
    ///
    /// </summary>
    [HttpPost]
    public IActionResult TryParseSouguuMaterials(string materials)
    {

        Console.WriteLine(materials);
        var souguuMaterials = JsonSerializer.Deserialize<SouguuWebsocketDto>(materials);
        Console.WriteLine("TryParseSouguuMaterials");
        if (souguuMaterials != null)
        {
            Console.WriteLine(souguuMaterials.incredients.Count);
            Console.WriteLine(souguuMaterials.incredients[0].type);
            return Ok(souguuMaterials);
        }

        return Ok(null);
    }

    [HttpGet]
    public IActionResult GetOnlineUsers()
    {
        return Ok(userOnlineConcurrentDictionaryDatabase.GetOnlineUsers());
    }

    [HttpGet]
    public IActionResult ClearUserOnline()
    {
        userOnlineConcurrentDictionaryDatabase.Clear();
        callingService.Clear();
        return Ok("UserOnlineDictionary is cleared");
    }

    [HttpGet]
    public IActionResult IsUserSouguu(int userId)
    {
        return Ok(userOnlineConcurrentDictionaryDatabase.IsUserSouguu(userId));
    }

    [HttpGet]
    public async Task<IActionResult> GetFriendsAndOnlineUsers(int userId)
    {
        Random random = new Random();
        var friends = (await userOnlineConcurrentDictionaryDatabase.GetFriendAndOnlineUsers(userId)).OrderBy((_) => random.Next());
        return Ok(friends);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetSouguuIncredients()
    {
        return Ok(souguuService.GetLatestIncredient());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForceSouguu(int user1, int user2)
    {
        souguuService.ForceSouguu(user1, user2);
        return Ok();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RemoveUserOnline(int userId)
    {
        userOnlineConcurrentDictionaryDatabase.RemoveUserOnline(userId);
        return Ok();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RemoveUserSouguu(int userId)
    {
        userOnlineConcurrentDictionaryDatabase.RemoveSouguu(userId);
        return Ok();
    }
}
