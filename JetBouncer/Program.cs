using System.Security.Claims;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Space.AspNetCore.Authentication;
using JetBrains.Space.AspNetCore.Authentication.Experimental.TokenManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using JetBouncer;
using Team23.TelegramSkeleton;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration((context, b) =>
{
  b.AddJsonFile(
    string.IsNullOrEmpty(context.HostingEnvironment.EnvironmentName)
      ? @$"appsettings.user.json"
      : @$"appsettings.{context.HostingEnvironment.EnvironmentName}.user.json", true);
});
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(b => b.RegisterModule<RegistrationModule>()));


// Add services to the container.
builder.Services
  .AddApplicationInsightsTelemetry()
  .AddMemoryCache()
  .Configure<Configuration>(builder.Configuration)
  .Configure<ForwardedHeadersOptions>(options =>
  {
    options.ForwardedHeaders = ForwardedHeaders.All;
  })
  .AddAuthentication(options =>
  {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = SpaceDefaults.AuthenticationScheme;
  })
  .AddCookie()
  .AddSpace(options =>
  {
    builder.Configuration.Bind("Space", options);
    options.RequestCredentials = RequestCredentials.Required;
    options.AccessType = AccessType.Online;
  });  

builder.Services
  .AddHttpContextAccessor()
  .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
  .AddScoped(x => x
    .GetRequiredService<IUrlHelperFactory>()
    .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext ?? new ActionContext()));

builder.Services
  .AddMvc()
  .AddNewtonsoftJson();

builder.Services.RegisterTelegramClients<TelegramBotClientEx, ITelegramBotClientEx>(serviceProvider =>
  serviceProvider.GetService<IOptions<Configuration>>()?.Value?.BotTokens);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
  ForwardedHeaders = ForwardedHeaders.All
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{ 
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
  app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
public class RegistrationModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterTelegramSkeleton<TelegramBotClientEx>();
  }
}