using BATTARI_api.Models;

namespace BATTARI_api.Interfaces;

public interface IUserRepository
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="id">indexIdでユーザーを取得します</param>
    ///
    /// <returns></returns> <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <exception
    /// cref="InvalidOperationException">ユーザーが存在しない時</exception>
    /// <returns></returns>
    Task<UserModel?> GetUser(int id);
    /// <summary>
    ///
    /// </summary>
    /// <param name="userId">userIdでユーザーを取得します</param>
    ///
    /// <returns></returns> <summary>
    ///
    /// </summary>
    /// <param name="userId"></param>
    /// <exception
    /// cref="InvalidOperationException">ユーザーが存在しない時</exception>
    /// <returns></returns>
    Task<UserModel?> GetUser(string userId);
    /// <summary>
    /// </summary>
    /// <param
    /// name="id">userIdからユーザーを取得し，見つけたユーザーのUserDtoを返すます</param>
    Task<UserDto?> GetUserDto(int id);
    Task<IEnumerable<UserModel>> GetUsers();
    Task<UserModel?> CreateUser(UserModel userRegisterModel);
    Task<UserModel> DeleteUser(int id);
    Task<bool> UserExists(string userId);
    Task<UserModel?> ChangeNickname(string userId, string nickname);
}
