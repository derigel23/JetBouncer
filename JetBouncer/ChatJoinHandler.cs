using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JetBouncer;

[UpdateHandler(UpdateTypes = new[] { UpdateType.ChatJoinRequest, UpdateType.MyChatMember })]
public class ChatJoinHandler : IUpdateHandler
{
  private readonly ITelegramBotClient myBot;
  private readonly IUrlHelper myUrlHelper;
  private readonly IMemoryCache myMemoryCache;

  public ChatJoinHandler(ITelegramBotClient bot, IUrlHelper urlHelper, IMemoryCache memoryCache)
  {
    myBot = bot;
    myUrlHelper = urlHelper;
    myMemoryCache = memoryCache;
  }
  
  public async Task<bool?> Handle(Update data, OperationTelemetry? context = default, CancellationToken cancellationToken = default)
  {
    if (data.ChatJoinRequest is {} chatJoinRequest)
    {
      var authId = Random.Shared.NextInt64();
      myMemoryCache.Set(authId, chatJoinRequest);
      var authLink = myUrlHelper.ActionLink("Auth", "Status", new { authId, myBot.BotId });
      await myBot.SendTextMessageAsync(chatJoinRequest.From.Id, authLink, cancellationToken: cancellationToken);
      return true;
    }

    if (data.MyChatMember is { } myMember)
    {

      if (myMember.NewChatMember is ChatMemberAdministrator { CanInviteUsers: true } administrator)
      {
        var inviteLink = await myBot.CreateChatInviteLinkAsync(myMember.Chat, createsJoinRequest: true, cancellationToken: cancellationToken);
        await myBot.SendTextMessageAsync(myMember.Chat.Id, inviteLink.InviteLink, cancellationToken: cancellationToken);
      }
      else if (myMember.NewChatMember.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator)
      {
        await myBot.SendTextMessageAsync(myMember.Chat.Id, "I'm should be added as an admin with invitation rights", cancellationToken: cancellationToken);
      }
      return true;
    }

    return null;
  }
}