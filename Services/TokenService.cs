using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BATTARI_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BATTARI_api.Services;

public class TokenService : ITokenService
{
    private readonly IRefreshTokensRepository RefreshTokensRepository;
    public TokenService(IRefreshTokensRepository refreshTokensRepository)
    {
        RefreshTokensRepository = refreshTokensRepository;
    }
    /// <summary>
    /// JWTTokenを生成します
    /// key, issuer, audienceはappsettings.jsonから取得します
    /// </summary>
    /// <param name="key"></param>
    /// 16バイト以上
    /// <param name="userModel"></param>
    /// JWTの有効期限
    /// <param name="expires"></param>
    /// <returns></returns>
    public string GenerateToken(string key, UserModel userModel,
                                DateTime? expires = null)
    {
        // 暗号化アルゴリズム(鍵の生成)
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        // 署名の作成
        var credentials =
            new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
      new Claim(JwtRegisteredClaimNames.Iss, "BATTARI-team"),
      new Claim(JwtRegisteredClaimNames.NameId, userModel.UserId),
      new Claim(JwtRegisteredClaimNames.Name, userModel.Name),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new Claim(JwtRegisteredClaimNames.UniqueName, userModel.Id.ToString())
    };

        var token = new JwtSecurityToken(
            claims: claims, expires: expires ?? DateTime.Now.AddDays(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    string generateRefreshToken()
    {

        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<string> GenerateAndSaveRefreshToken(UserModel userModel)
    {
        string token = generateRefreshToken();
        try
        {
            await RefreshTokensRepository.Add(
                new RefreshTokenModel(token: token, userId: userModel.Id,
                                      expires: DateTime.Now.AddDays(365),
                                      created: DateTime.Now, isActive: true));
        }
        catch (DbUpdateException e)
        {
            throw new Exception("RefreshTokenの保存に失敗しました(" +
                                    "データベースへのアクセスに失敗しました)",
                                e);
        }
        catch (Exception e)
        {
            throw new Exception("RefreshTokenの保存に失敗しました", e);
        }
        return token;
    }

    public async Task<RefreshTokenModel> ValidateRefreshToken(string token)
    {
        return await RefreshTokensRepository.GetByToken(token);
    }
}
