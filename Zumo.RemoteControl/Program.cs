using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zumo.Library;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Execution;
using Zumo.RemoteControl.Lidar;
using Zumo.RemoteControl.Logging;
using Zumo.RemoteControl.Routes;
using Zumo.RemoteControl.Runtime;
using Zumo.RemoteControl.Servers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.IncludeScopes = false;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.Configure<RemoteControlOptions>(builder.Configuration.GetSection("RemoteControl"));
builder.Services.Configure<ZumoRuntimeOptions>(builder.Configuration.GetSection("ZumoRuntime"));

builder.Services.AddSingleton<IZumoRuntimeFactory, ZumoRuntimeFactory>();
builder.Services.AddSingleton<ILidarAnalyzer, BasicLidarAnalyzer>();
builder.Services.AddSingleton<IRouteCatalog, RouteCatalog>();
builder.Services.AddSingleton<IRouteExecutor, RouteExecutor>();
builder.Services.AddSingleton<ILogWriter, LogWriter>();

builder.Services.AddHostedService<TcpControlServer>();
builder.Services.AddHostedService<HttpFileServer>();

var app = builder.Build();
await app.RunAsync();
