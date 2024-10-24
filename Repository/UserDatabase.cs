using System.Drawing.Printing;
using BATTARI_api.Data;
using BATTARI_api.Interfaces;
using BATTARI_api.Migrations;
using BATTARI_api.Models;
using Microsoft.EntityFrameworkCore;
using webUserLoginTest.Util;

namespace BATTARI_api.Repository;

public class UserDatabase
(UserContext userContext) : IUserRepository
{
    const string _pepper = "BATTARI";
    private UserContext _userContext = userContext;

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<UserModel?> GetUser(int id)
    {
        return await _userContext.Users.FindAsync(id);
    }

    public async Task<UserDto?> GetUserDto(int id)
    {
        UserModel? user = await _userContext.Users.FindAsync(id);
        if (user == null)
            return null;
        return new UserDto()
        {
            UserId = user.UserId,
            Name = user.Name,
            Id = user.Id,
        };
    }

    public async Task<UserModel?> CreateUser(UserModel userModel)
    {
        // //ここら辺はコアロジック？
        // DateTime _created = DateTime.Now;
        // byte[] _salt = PasswordUtil.GetInitialPasswordSalt(_created.ToString());
        // UserModel userModel = new UserModel()
        // {
        //     UserId = userRegisterModel.UserId,
        //     Name = userRegisterModel.Name,
        //     PasswordHash = PasswordUtil.GetPasswordHashFromPepper(_salt,
        //     userRegisterModel.Password, _pepper), PasswordSalt = _salt, Created =
        //     _created
        // };

        var result = await _userContext.AddAsync(userModel);
        Console.WriteLine(result);

        try
        {
            await _userContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
        return userModel;
    }

    public async Task<UserModel?> GetUser(string userId)
    {
        Console.WriteLine(userId);
        UserModel user;
        try
        {
            user = await _userContext.Users.Where<UserModel>(x => x.UserId == userId)
                       .FirstAsync();
        }
        catch (InvalidOperationException e)
        {
            return null;
        }
        Console.WriteLine(user.Created);
        return user;
    }

    public async Task<UserModel> DeleteUser(int id)
    {
        UserModel user = await _userContext.Users.FindAsync(id);
        if (user != null)
        {
            _userContext.Users.Remove(user);
            await _userContext.SaveChangesAsync();
            return user;
        }
        else
        {
            throw new Exception("User not found");
        }
    }

    public async Task<IEnumerable<UserModel>> GetUsers()
    {
        return await _userContext.Users.ToListAsync();
    }

    public async Task<bool> UserExists(string userId)
    {
        return await _userContext.Users.AnyAsync(x => x.UserId == userId);
    }

    // edit
    public async Task<UserModel
        ?> ChangeNickname(string userId, string nickname)
    {
        UserModel? user = null;
        try
        {
            user = await _userContext.Users.Where<UserModel>(x => x.UserId == userId)
                       .FirstAsync();
        }
        catch (ArgumentNullException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Exception e)
        {
            throw;
        }
        if (user == null)
            return null;
        user.Name = nickname;
        _userContext.Update(user);
        await _userContext.SaveChangesAsync();
        return user;
    }
}
