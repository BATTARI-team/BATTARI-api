using System.Security.Claims;
using BATTARI_api.Interfaces;
using BATTARI_api.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class FriendController
(IUserRepository _userRepository, IFriendRepository _friendRepository)
    : Controller
{

    [HttpPost]
    public async Task<ActionResult<FriendRequestDto>> PushFriendRequest(
        int userIndex)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var claim = HttpContext.User.Claims.FirstOrDefault(
            c => c.Type ==
                 "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        if (claim == null)
            return BadRequest();
        int claimId;
        try
        {
            claimId = int.Parse(claim.Value);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
        try
        {
            FriendStatusEnum? friendStatusEnum =
                await _friendRepository.AddFriendRequest(claimId, userIndex);
            if (friendStatusEnum == null)
                return BadRequest();
            else
            {
                return new FriendRequestDto()
                {
                    User1 = claimId,
                    User2 = userIndex,
                    Status = (FriendStatusEnum)friendStatusEnum
                };
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetFriendList()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var claim = HttpContext.User.Claims.FirstOrDefault(
            c => c.Type ==
                 "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        if (claim == null)
            return BadRequest();
        int claimId;
        try
        {
            claimId = int.Parse(claim.Value);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
        try
        {
            IEnumerable<UserDto> friendList =
                await _friendRepository.GetFriendList(claimId);
            return Ok(friendList);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
    }

    [HttpGet]
    public async Task<ActionResult<FriendRequestDto>> GetFriendRequest(
        int userIndex)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var claim = HttpContext.User.Claims.FirstOrDefault(
            c => c.Type ==
                 "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        if (claim == null)
            return BadRequest("tokenが無効です");
        int claimId;
        try
        {
            claimId = int.Parse(claim.Value);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
        try
        {
            FriendModel? friendModel =
                await _friendRepository.IsExist(claimId, userIndex);
            if (friendModel == null)
                return new FriendRequestDto()
                {
                    User1 = claimId,
                    User2 = userIndex,
                    Status = FriendStatusEnum.none
                };
            else
            {
                return new FriendRequestDto()
                {
                    User1 = claimId,
                    User2 = userIndex,
                    Status = friendModel.Status
                };
            }
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
    }
}
