using CodeCompilerService;
using CodeCompilerService.OptionModels;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext ,services) =>
    {
        services.AddHostedService<Worker>();
        services.AddLogging(configure => configure.AddSerilog());

        IConfiguration configuration = hostContext.Configuration;
        WorkerServiceOptions serviceOptions = configuration.GetSection("ServiceOptions").Get<WorkerServiceOptions>();
        CodeCompilerLibOptions codeCompilerLibOptions = configuration.GetSection("CodeCompilerLibOptions").Get<CodeCompilerLibOptions>();
        services.AddSingleton(codeCompilerLibOptions);
        services.AddSingleton(serviceOptions);
    })
    .UseWindowsService()
    .UseSerilog((hostingContext, loggerConfiguration) =>
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
    .Build();

await host.RunAsync();
