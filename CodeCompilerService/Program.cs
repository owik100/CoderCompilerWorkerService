using CodeCompilerService;
using CodeCompilerService.OptionModels;
using Microsoft.Extensions.Logging.EventLog;

IHost host = Host.CreateDefaultBuilder(args)
     .ConfigureLogging((context, logging) =>
     {
         logging.AddEventLog(new EventLogSettings()
         {
             LogName = "CodeCompilerServiceLogging",
         });
     })
    .ConfigureServices((hostContext ,services) =>
    {
        services.AddHostedService<Worker>();

        IConfiguration configuration = hostContext.Configuration;
        WorkerServiceOptions serviceOptions = configuration.GetSection("ServiceOptions").Get<WorkerServiceOptions>();
        CodeCompilerLibOptions codeCompilerLibOptions = configuration.GetSection("CodeCompilerLibOptions").Get<CodeCompilerLibOptions>();
        services.AddSingleton(codeCompilerLibOptions);
        services.AddSingleton(serviceOptions);
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
