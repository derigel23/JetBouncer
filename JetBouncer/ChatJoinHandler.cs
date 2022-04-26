using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JetBouncer;

[UpdateHandler(UpdateTypes = new[] { UpdateType.ChatJoinRequest, UpdateType.MyChatMember, UpdateType.Message })]
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
    if (data.Message is { Entities: { Length: > 0 } entities } message)
    {
      foreach (var entity in entities)
      {
        var messageEntity = new MessageEntityEx(message, entity);
        if (messageEntity.Type == MessageEntityType.BotCommand &&
            string.Equals(messageEntity.Value.ToString(), "/start", StringComparison.OrdinalIgnoreCase))
        {
          if (messageEntity.AfterValue.Length == 0)
          {
            var botInfo = await myBot.GetMeAsync(cancellationToken);
            var addLink = new Uri($"https://t.me/{botInfo.Username}?startgroup=init&admin=invite_users");
            await myBot.SendVideoAsync(message.Chat.Id, "https://telegram.org/file/464001508/10265/9s2PGXyzQW0.3317857.mp4/f2d60efe6ca5d1fae2",
              caption: "Add me to the necessary group or channel as an administrator with \"Invite Users via Link\" rights.",
              replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Add me", addLink.AbsoluteUri)),
              cancellationToken: cancellationToken);
            return true;
          }
        }
      }
    }
    
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
        var inviteLink = await myBot.CreateChatInviteLinkAsync(myMember.Chat.Id, createsJoinRequest: true, cancellationToken: cancellationToken);
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