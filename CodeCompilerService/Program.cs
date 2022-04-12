using CodeCompilerService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext ,services) =>
    {
        services.AddHostedService<Worker>();

        IConfiguration configuration = hostContext.Configuration;
        WorkerOptions options = configuration.GetSection("Options").Get<WorkerOptions>();
        services.AddSingleton(options);
    })
    .Build();

await host.RunAsync();
