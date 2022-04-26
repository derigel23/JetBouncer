using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace JetBouncer;

public class StatusController : StatusController<object, bool?, IBotCommandHandlerAttribute<object>>
{
  private readonly IMemoryCache myMemoryCache;
  private readonly IDictionary<long, ITelegramBotClient> myBots;

  public StatusController(IMemoryCache memoryCache, IDictionary<long, ITelegramBotClient> bots, IEnumerable<IStatusProvider> statusProviders)
    : base(null, bots.Values, statusProviders, null)
  {
    myMemoryCache = memoryCache;
    myBots = bots;
  }

  [HttpGet("/auth"), Authorize]
  public async Task<IActionResult> Auth(long authId, long botId, CancellationToken cancellationToken = default)
  {
    if (myMemoryCache.TryGetValue<ChatJoinRequest>(authId, out var request) &&
        myBots.TryGetValue(botId, out var bot))
    {
      myMemoryCache.Remove(authId);
      await bot.ApproveChatJoinRequest(request.Chat.Id, request.From.Id, cancellationToken);
      return Redirect(request.InviteLink?.InviteLink);
    }

    return NotFound();
  }

  public override async Task<IActionResult> Refresh(CancellationToken cancellationToken)
  {
    var result = await base.Refresh(cancellationToken);
    foreach (var bot in myBots.Values)
    {
      var administratorRights = new ChatAdministratorRights { CanManageChat = true, CanInviteUsers = true };
      await bot.SetMyDefaultAdministratorRightsAsync(administratorRights, false, cancellationToken);
      await bot.SetMyDefaultAdministratorRightsAsync(administratorRights, true, cancellationToken);
    }
    return result;
  }
}