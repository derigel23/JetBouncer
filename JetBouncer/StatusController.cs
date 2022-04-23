using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace JetBouncer;

public class StatusController : StatusController<object, bool?, BotCommandAttribute>
{
  private readonly IMemoryCache myMemoryCache;
  private readonly IDictionary<long, ITelegramBotClient> myBots;

  public StatusController(IMemoryCache memoryCache, IDictionary<long, ITelegramBotClient> bots, IEnumerable<IStatusProvider> statusProviders, IEnumerable<Lazy<Func<Message, IBotCommandHandler<object, bool?>>, BotCommandAttribute>> commandHandlers)
    : base(null, bots.Values, statusProviders, commandHandlers)
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
}