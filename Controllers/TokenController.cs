using BATTARI_api.Interfaces;
using BATTARI_api.Models;
using BATTARI_api.Services;
using Microsoft.AspNetCore.Mvc;
using webUserLoginTest.Util;

namespace BATTARI_api.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class TokenController(ITokenService tokenService, IConfiguration configuration, IUserRepository userControllerInterface) : ControllerBase
{
    private readonly ITokenService tokenService = tokenService;
    private readonly IConfiguration configuration = configuration;
    private readonly IUserRepository userControllerInterface = userControllerInterface;

    /// <summary>
    /// UserIdとPasswordがあっていれば，Tokenを返します
    /// </summary>
    /// <param name="userLoginModel"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] UserLoginModel userLoginModel)
    {
        UserModel? user = await userControllerInterface.GetUser(userLoginModel.UserId);
        if (user == null)
        {
            return NotFound("User not found");
        }
        //TODO パスワードペッパーを別の場所に保管する．
        if (PasswordUtil.CompareHash(user.PasswordHash, PasswordUtil.GetPasswordHashFromPepper(user.PasswordSalt, userLoginModel.Password, "BATTARI")))
        {
            var token = tokenService.GenerateToken(
                configuration["Jwt:Key"] ?? "",
                user
            );
            return Ok(token);
        }
        else
        {
            return Unauthorized();
        }
    }

}