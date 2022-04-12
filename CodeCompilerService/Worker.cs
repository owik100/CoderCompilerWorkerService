namespace CodeCompilerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerOptions _options;

        private FileSystemWatcher fileWatcher;

        public Worker(ILogger<Worker> logger, WorkerOptions options)
        {
            _logger = logger;
            _options = options;
            fileWatcher = new FileSystemWatcher();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{DateTime.Now}: CodeCompilerService still alive...");
                await Task.Delay(_options.Interval, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now}: Starting CodeCompilerService");

            try
            {
                fileWatcher.Path = _options.InputPath;
                fileWatcher.Created += FileWatcher_FileCreated;
                fileWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return base.StartAsync(cancellationToken);
        }

        private void FileWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now}: File {e.Name} was added to input folder");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now}: ENDING CodeCompilerService");
            return base.StopAsync(cancellationToken);
        }
    }
}