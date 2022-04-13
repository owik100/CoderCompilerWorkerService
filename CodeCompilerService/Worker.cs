using CodeCompilerNs;
using CodeCompilerService.OptionModels;
using System.Text;

namespace CodeCompilerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerServiceOptions _serviceOptions;
        private readonly CodeCompilerLibOptions _codeCompilerLibOptions;

        private FileSystemWatcher fileWatcher;
        private CodeCompiler codeCompiler;
        private bool firstCall = true;

        public Worker(ILogger<Worker> logger, WorkerServiceOptions serviceOptions, CodeCompilerLibOptions codeCompilerLibOptions)
        {
            _logger = logger;
            _serviceOptions = serviceOptions;
            _codeCompilerLibOptions = codeCompilerLibOptions;
            fileWatcher = new FileSystemWatcher();

            codeCompiler = new CodeCompiler();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if(!firstCall)
                        _logger.LogInformation($"{DateTime.Now}: CodeCompilerService still alive...");

                    firstCall = false;
                    await Task.Delay(_serviceOptions.Interval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now}: Starting CodeCompilerService...");
            try
            {
                fileWatcher.Path = _codeCompilerLibOptions.InputPath;
                fileWatcher.Created += FileWatcher_FileCreated;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.InternalBufferSize = _serviceOptions.InternalBufferSize;
                _logger.LogInformation($"{DateTime.Now}: CodeCompilerService listening for files in directory {fileWatcher.Path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return base.StartAsync(cancellationToken);
        }

        private void FileWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now}: File {e.Name} was added to input folder");
                var res = codeCompiler.CreateAssemblyToPath(e.FullPath, _codeCompilerLibOptions.OutputPath);
                if (res.Success)
                {
                    _logger.LogInformation($"{DateTime.Now}: File {e.Name} compiled correctly");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in res.Diagnostics)
                    {
                        sb.Append(item.GetMessage());
                        sb.Append(Environment.NewLine);
                    }
                    _logger.LogWarning($"{DateTime.Now}: File {e.Name} compiled with errors: {sb.ToString()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now}: ENDING CodeCompilerService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return base.StopAsync(cancellationToken);
        }
    }
}