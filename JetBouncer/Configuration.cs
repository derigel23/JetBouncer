using JetBrains.Annotations;

namespace JetBouncer;

public class Configuration
{
  public string[]? BotTokens { get; [UsedImplicitly] set; }
}