using System.Collections.Concurrent;
using System.Drawing.Printing;
using System.Text.Json;
using BATTARI_api.Models;
using BATTARI_api.Models.Data;
using BATTARI_api.Models.DTO;
using BATTARI_api.Models.Enum;
using BATTARI_api.Models.Log;
using BATTARI_api.Repository;
using BATTARI_api.Services;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;
using Sentry;

public interface ISouguuService
{
    public Task AddMaterial(SouguuWebsocketDto materials);
    public void AddSouguuNotification(string requestId,
                                      Action<SouguuNotificationDto> action,
                                      int userIndex);
    /// <summary>
    ///
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns>もし他にこのユーザーへのwebsocket接続が存在しなかった場合はtrueを返す．つまりオンラインユーザーから省くべき場合はtrue</returns>
    public bool RemoveSouguuNotification(string requestId);
    public Dictionary<int, SouguuWebsocketDto> GetLatestIncredient();
    public void ForceSouguu(int user1, int user2);
    public Task RemoveMaterial(int userId);
    public Task CancelCall(int userIndex,
                           CancelCallWebsocketDto cancelCallWebsocketDto);
    public Task AddCancelNotification(string requestId,
                                      Action<int, CancelCallWebsocketDto> action);
    public Task RemoveCancelNotification(string requestId);
}

public class SouguuService : ISouguuService
{
    private readonly UserOnlineConcurrentDictionaryDatabase
        _userOnlineConcurrentDictionaryDatabase;
    private readonly ICallRepository _callRepository;
    private readonly CallingService _callingService;
    private readonly ILogger<ISouguuService> _logger;
    public SouguuService(UserOnlineConcurrentDictionaryDatabase
                             userOnlineConcurrentDictionaryDatabase,
                         CallingService callingService,
                         ILogger<ISouguuService> logger,
                         IServiceScopeFactory serviceScopeFactory)
    {
        CreateDequeTask();
        _userOnlineConcurrentDictionaryDatabase =
            userOnlineConcurrentDictionaryDatabase;
        _callingService = callingService;
        _callRepository =
            serviceScopeFactory.CreateScope()
                .ServiceProvider.GetRequiredService<ICallRepository>();
        _logger = logger;
        Console.WriteLine("SouguuServiceが作成されました");
    }
    /// <summary>
    /// 遭遇判定するためのキュー
    /// </summary>
    private readonly ConcurrentQueue<int> _souguuQueue =
        new ConcurrentQueue<int>();
    public ConcurrentDictionary<int, SouguuWebsocketDto> _latestIncredient =
        new ConcurrentDictionary<int, SouguuWebsocketDto>();
    private ConcurrentDictionary<
        string, Action<int, CancelCallWebsocketDto>> _callCancelNotification =
        new ConcurrentDictionary<string, Action<int, CancelCallWebsocketDto>>();
    private ConcurrentDictionary<string, Action<SouguuNotificationDto>>
        _souguuNotification =
            new ConcurrentDictionary<string, Action<SouguuNotificationDto>>();
    private ConcurrentDictionary<string, int> _requestIdToUserIndex =
        new ConcurrentDictionary<string, int>();

    private Task _dequeueTask;

    public void AddSouguuNotification(string requestId,
                                      Action<SouguuNotificationDto> action,
                                      int userIndex)
    {
        _souguuNotification.AddOrUpdate(requestId, i => action,
                                        (i, action1) => action);
        _requestIdToUserIndex.AddOrUpdate(requestId, userIndex,
                                          (i, index) => userIndex);
    }

    public bool RemoveSouguuNotification(string requestId)
    {
        _souguuNotification.TryRemove(requestId, out _);
        var userIndex = _requestIdToUserIndex[requestId];
        _requestIdToUserIndex.TryRemove(requestId, out _);
        return _requestIdToUserIndex.Any(i => i.Value == userIndex);
    }

    /// <summary>
    /// 遭遇の判断材料を入れる
    /// </summary>
    /// <param name="materials"></param>
    /// <param name="userIndex"></param>
    public async Task AddMaterial(SouguuWebsocketDto materials)
    {
        _latestIncredient[materials.id] = materials;
        Console.WriteLine("追加されました　from " + materials.id);
        SentrySdk.CaptureMessage("追加されました　from " + materials.id,
                                 SentryLevel.Debug);
        AddSouguuQueueElement(materials.id);
        Console.WriteLine(_latestIncredient.Count);
    }

    public async Task RemoveMaterial(int userId)
    {
        var isSuc = _latestIncredient.TryRemove(userId, out _);
        if (!isSuc)
        {
            throw new Exception("遭遇材料の削除に失敗しました");
        }
        SentrySdk.CaptureMessage(userId + "nの遭遇材料が削除されました",
                                 SentryLevel.Debug);
    }

    public async Task CancelCall(int userIndex,
                                 CancelCallWebsocketDto cancelCallWebsocketDto)
    {
        var call = _callingService.GetCall(userIndex);
        if (call == null)
        {
            throw new Exception("通話が見つかりませんでした");
        }
        var requestIdsToNotify = _requestIdToUserIndex.Where(i => (i.Value == userIndex) || (i.Value == call.AiteUserId))
                                     .Select(i => i.Key)
                                     .ToList();
        requestIdsToNotify.ForEach((requestId) =>
                                   {
                                       _callCancelNotification.TryGetValue(requestId, out var action);
                                       if (action != null)
                                       {
                                           action(userIndex, cancelCallWebsocketDto);
                                       }
                                   });
    }

    public async
        Task AddCancelNotification(string requestId,
                                   Action<int, CancelCallWebsocketDto> action)
    {
        _callCancelNotification.AddOrUpdate(requestId, i => action,
                                            (i, action1) => action);
    }

    public async Task RemoveCancelNotification(string requestId)
    {
        _callCancelNotification.TryRemove(requestId, out _);
    }

    public Dictionary<int, SouguuWebsocketDto> GetLatestIncredient()
    {
        return new Dictionary<int, SouguuWebsocketDto>(_latestIncredient);
    }

    private void AddSouguuQueueElement(int userIndex)
    {
        _souguuQueue.Enqueue(userIndex);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="user1"></param>
    /// <param name="user2"></param>
    /// <param name="reason"></param>
    private async Task Souguu(int user1, int user2, SouguuReasonStatusEnum reason,
                              string reasonStr)
    {
        Console.WriteLine("遭遇しました");
        CallModel call;

        // 配列{channelId, user1Token, user2Token)
        string[] callDetail;
        try
        {
            call = await _callRepository.AddCall(
                souguuReason: reasonStr, callStartTime: DateTime.Now.AddSeconds(15),
                user1: user1, user2: user2, souguuDateTime: DateTime.Now,
                status: CallStatusEnum.Waiting);
            callDetail = await  _callingService.AddCall(
                callId: call.CallId, callStartTime: call.CallStartTime,
                callEndTime: call.CallStartTime.AddMinutes(call.CallTime),
                souguuReason: call.SouguuReason, user1: call.User1Id,
                user2: call.User2Id, cancellationReason: "",
                souguuDateTime: call.SouguuDateTime);
        }
        catch (DbUpdateException e)
        {
            Console.WriteLine("データベースに保存できませんでした" + e);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError("遭遇処理に失敗しました", e);
            throw;
        }

        _userOnlineConcurrentDictionaryDatabase.SetSouguu(user1, user2);
        var user1RequestIds = _requestIdToUserIndex.Where(i => i.Value == user1)
                                  .Select(i => i.Key)
                                  .ToList();
        var user2RequestIds = _requestIdToUserIndex.Where(i => i.Value == user2)
                                  .Select(i => i.Key)
                                  .ToList();
        if (user1RequestIds.Count == 0 || user2RequestIds.Count == 0)
        {
            _logger.LogWarning(
                "遭遇通知が送信されませんでした, user1: {}: {}, user2: {}: {}", user1,
                user2, user1RequestIds.Count, user2RequestIds.Count);
            SentrySdk.CaptureMessage(
                "遭遇通知が送信されませんでした, user1: " + user1 + ":" + user2 +
                    ", user2: " + user2 + ":" + user2RequestIds.Count,
                SentryLevel.Warning);
        }
        foreach (var user1RequestId in user1RequestIds)
        {
            _souguuNotification.TryGetValue(user1RequestId,
                                            out var user1Notification);
            if (user1Notification != null)
            {
                user1Notification(new SouguuNotificationDto()
                {
                    CallEndTime = call.CallStartTime.AddMinutes(call.CallTime),
                    CallStartTime = call.CallStartTime,
                    CallId = call.CallId,
                    SouguuDateTime = call.SouguuDateTime,
                    SouguuReason = call.SouguuReason,
                    Token = callDetail[1],
                    AiteUserId = user2
                });
            }
        }

        foreach (var user2RequestId in user2RequestIds)
        {
            _souguuNotification.TryGetValue(user2RequestId,
                                            out var user2Notification);
            if (user2Notification != null)
            {
                user2Notification(new SouguuNotificationDto()
                {
                    CallEndTime = call.CallStartTime.AddMinutes(call.CallTime),
                    CallStartTime = call.CallStartTime,
                    CallId = call.CallId,
                    SouguuDateTime = call.SouguuDateTime,
                    SouguuReason = call.SouguuReason,
                    Token = callDetail[2],
                    AiteUserId = user1
                });
            }
        }

        SentrySdk.CaptureMessage(user1 + "と" + user2 + "が遭遇しました⭐⭐️⭐️️",
                                 SentryLevel.Info);
        _logger.LogInformation("{}と{}が遭遇しました⭐⭐️⭐️️", user1, user2);
        _userOnlineConcurrentDictionaryDatabase.SetLastSouguu(user1);
        _userOnlineConcurrentDictionaryDatabase.SetLastSouguu(user2);
    }

    // #TODO 遭遇材料が古かったら，どうしよう
    private async Task SouguuCheck(int user1, int user2)
    {
        _logger.LogInformation("遭遇判定: {user1}と{user2}", user1, user2);
        SentrySdk.CaptureMessage("遭遇判定: " + user1 + "と" + user2,
                                 SentryLevel.Debug);
        if (!_latestIncredient.ContainsKey(user2))
            return;

        var user1Materials = _latestIncredient[user1];
        var user2Materials = _latestIncredient[user2];

        var user1OnlineUser = _userOnlineConcurrentDictionaryDatabase[user1];
        var user2OnlineUser = _userOnlineConcurrentDictionaryDatabase[user2];
        if (user1OnlineUser == null || user2OnlineUser == null)
            return;
        // 連続して遭遇しすぎないように20分間のタイムアウトを設ける
        if (user1OnlineUser.LastSouguuTime > DateTime.Now.AddMinutes(-1) ||
            user2OnlineUser.LastSouguuTime > DateTime.Now.AddMinutes(-1))
            return;

        SouguuReasonStatusEnum? result = null;

        if (user1Materials.isWelcome && user2Materials.isWelcome)
        {
            // ここで遭遇処理を行う
            await Souguu(user1, user2, SouguuReasonStatusEnum.Battari_Welcome,
                         "BATTARI WelcomeでBATTARI");
            result = SouguuReasonStatusEnum.Battari_Welcome;
        }

        const int souguu_app_usage_time = 0;

        SouguuAppIncredientModel? user1AppUsage = null;
        foreach (var VARIABLE in user1Materials.incredients)
        {
            if (VARIABLE is not SouguuAppIncredientModel model)
                continue;
            if (model.appData.useTime <= souguu_app_usage_time)
                continue;

            user1AppUsage = model;
            break;
        }

        SouguuAppIncredientModel? user2AppUsage = null;
        foreach (var VARIABLE in user2Materials.incredients)
        {
            if (VARIABLE is not SouguuAppIncredientModel model)
                continue;
            if (model.appData.useTime <= souguu_app_usage_time)
                continue;

            user2AppUsage = model;
            break;
        }
        if (user1AppUsage == null || user2AppUsage == null)
            return;

        if (String.Compare(user1AppUsage.appData.appName,
                           user2AppUsage.appData.appName,
                           StringComparison.Ordinal) == 0)
        {
            await Souguu(user1, user2, SouguuReasonStatusEnum.App_Usage,
                         user1AppUsage.appData.appName + "でBATTARI");
            result = SouguuReasonStatusEnum.App_Usage;
        }

        string resultString = "";
        if (result == null)
            resultString = "";
        else
            resultString = result.ToString();
        var logmodel =
            (new CheckSouguuLogElement("遭遇判定", user1, user1Materials, user2,
                                       user2Materials, resultString))
                .ToString();
        _logger.LogInformation("遭遇判定終了: {user1}と{user2} {result}", user1,
                               user2, result);
        _logger.LogInformation(JsonSerializer.Serialize(logmodel));
    }

    private void CreateDequeTask()
    {
        Random random = new Random();
        _dequeueTask = Task.Run(async () =>
                                {
                                    while (true)
                                    {
                                        foreach (var VARIABLE in _callingService.GetNowCalls())
                                        {
                                            Console.WriteLine("call: " + VARIABLE.CallId + " " + VARIABLE.User1 +
                                                          " " + VARIABLE.User2);
                                        }

                                        if (_souguuQueue.TryDequeue(out int element))
                                        {
                                            if (element == 0)
                                                continue;
                                            // ここで遭遇処理を行う
                                            var friends = (await _userOnlineConcurrentDictionaryDatabase
                                                           .GetFriendAndOnlineUsers(element))
                                                          .OrderBy((_) => random.Next());
                                            if (!friends.Any())
                                                continue;
                                            foreach (var VARIABLE in friends)
                                            {
                                                Console.WriteLine(VARIABLE.Name);
                                                if (_userOnlineConcurrentDictionaryDatabase.IsUserSouguu(
                                                    VARIABLE.Id) == 0)
                                                {
                                                    await SouguuCheck(element, VARIABLE.Id);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Thread.Sleep(1000);
                                        }
                                    }
                                });
        _dequeueTask.ContinueWith(task => { CreateDequeTask(); });
    }
    public void ForceSouguu(int user1, int user2)
    {
        Souguu(user1, user2, SouguuReasonStatusEnum.Battari_Welcome,
               "強制遭遇しました");
    }
}
