using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
    if (data.ChatJoinRequest is { InviteLink.Creator: {} invitationCreator  } chatJoinRequest && invitationCreator.Id == myBot.BotId)
    {
      var authId = Random.Shared.NextInt64();
      myMemoryCache.Set(authId, chatJoinRequest);
      IReplyMarkup markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Authorize",
        myUrlHelper.ActionLink("Auth", "Status", new { authId, myBot.BotId })!));
      await myBot.SendTextMessageAsync(chatJoinRequest.From.Id, "Please, authorize to get access.", replyMarkup: markup, cancellationToken: cancellationToken);
      return true;
    }

    if (data.MyChatMember is { } myMember)
    {

      if (myMember.NewChatMember is ChatMemberAdministrator { CanInviteUsers: true })
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