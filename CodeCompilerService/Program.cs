using CodeCompilerService;
using CodeCompilerService.OptionModels;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext ,services) =>
    {
        services.AddHostedService<Worker>();

        IConfiguration configuration = hostContext.Configuration;
        WorkerServiceOptions serviceOptions = configuration.GetSection("ServiceOptions").Get<WorkerServiceOptions>();
        CodeCompilerLibOptions codeCompilerLibOptions = configuration.GetSection("CodeCompilerLibOptions").Get<CodeCompilerLibOptions>();
        services.AddSingleton(codeCompilerLibOptions);
        services.AddSingleton(serviceOptions);
    })
    .Build();

await host.RunAsync();
