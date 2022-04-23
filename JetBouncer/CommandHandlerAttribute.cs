using JetBrains.Annotations;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace JetBouncer
{ 
  [MeansImplicitUse]
  public class BotCommandAttribute : Attribute, IBotCommandHandlerAttribute<object>
  {
    public bool ShouldProcess(MessageEntityEx data, object? context) =>
      BotCommandHandler.ShouldProcess(this, data, context);

    public int Order { get; }
    public BotCommandScope[] Scopes { get; set; }
    public BotCommand Command { get; set; }
    public string[]? Aliases { get; set; }
  }
}