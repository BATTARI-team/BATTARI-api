using BATTARI_api.Data;
using BATTARI_api.Interfaces;
using BATTARI_api.Models;
using Microsoft.EntityFrameworkCore;

namespace BATTARI_api.Repository
{
    public interface IFriendRepository
    {
        Task<FriendModel?> IsExist(int user1, int user2);
        Task<FriendStatusEnum?> AddFriendRequest(int user1, int user2);
        Task<IEnumerable<UserDto>> GetFriendList(int userId);
    }

    public class FriendDatabase : IFriendRepository
    {
        private readonly UserContext _context;
        private readonly IUserRepository _userRepository;
        public FriendDatabase(UserContext context, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public async Task<FriendStatusEnum?> AddFriendRequest(int user1, int user2)
        {

            using var transaction = _context.Database.BeginTransaction();
            FriendModel? friendModel = await this.IsExist(user1, user2);
            if (friendModel != null)
            {
                if (friendModel.Status == FriendStatusEnum.accepted)
                    throw new Exception("すでに友達です");
                if (friendModel.User1Id == user1)
                    throw new Exception("すでに友達申請済みです");
                if (friendModel.User2Id == user1)
                {
                    friendModel.Status = FriendStatusEnum.accepted;
                    _context.Friends.Update(friendModel);
                    try
                    {
                        // saveChangesのエラー処理用の何かを用意するべき

                        await _context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return null;
                    }

                    transaction.Commit();
                    return FriendStatusEnum.accepted;
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                FriendModel newFriend =
                    new FriendModel()
                    {
                        User1Id = user1,
                        User2Id = user2,
                        Status = FriendStatusEnum.requested
                    };
                _context.Friends.Add(newFriend);
                try
                {
                    // saveChangesのエラー処理用の何かを用意するべき

                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
                transaction.Commit();
                return FriendStatusEnum.requested;
            }
        }

        public async Task<IEnumerable<UserDto>> GetFriendList(int userId)
        {
            var friendModel = await _context.Friends
                                  .Where(x => x.User1Id == userId ||
                                              x.User2Id == userId &&
                                                  x.Status == FriendStatusEnum.accepted)
                                  .ToListAsync();
            List<Task<UserDto
                ?>>? friendIdList =
                      friendModel.Where(x => x.Status == FriendStatusEnum.accepted)
                          .Select(async x =>
                          {
                              UserModel? user;
                              if (x.User1Id == userId)
                                  user = await _userRepository.GetUser(x.User2Id);
                              else
                                  user = await _userRepository.GetUser(x.User1Id);
                              if (user == null)
                                  return null;
                              return new UserDto()
                              {
                                  Id = user.Id,
                                  UserId = user.UserId,
                                  Name = user.Name,
                              };
                          })
                          .ToList();

            var friendListContainsNull = await Task.WhenAll(friendIdList);
            if (friendListContainsNull == null)
                friendListContainsNull = [];
            IEnumerable<UserDto> friendList =
                friendListContainsNull.Where(x => x != null);
            return friendList.AsEnumerable<UserDto>();
        }

        public async Task<FriendModel?> IsExist(int user1, int user2)
        {
            FriendModel? result = await _context.Friends.FirstOrDefaultAsync(
                x => (x.User1Id == user1 && x.User2Id == user2) ||
                     (x.User1Id == user2 && x.User2Id == user1));
            if (result == null)
                return null;
            else
                return result;
        }
    }
}
