using CodeCompilerNs;
using CodeCompilerService.OptionModels;
using Microsoft.CodeAnalysis.CSharp;
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

        ConnectionManagerServer server;

        public Worker(ILogger<Worker> logger, WorkerServiceOptions serviceOptions, CodeCompilerLibOptions codeCompilerLibOptions)
        {
            _logger = logger;
            _serviceOptions = serviceOptions;
            _codeCompilerLibOptions = codeCompilerLibOptions;
            fileWatcher = new FileSystemWatcher();

            Microsoft.CodeAnalysis.OutputKind outputKind = Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
            if (_codeCompilerLibOptions.BuildToConsoleApp)
            {
                outputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
            }
            else
            {
                outputKind = Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
            }
            codeCompiler = new CodeCompiler(cSharpCompilationOptions: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(outputKind));
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!firstCall)
                    {
                        _logger.LogInformation("CodeCompilerService still alive...");
                        server?.SendToClient("CodeCompilerService still alive...");
                    }


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
            if (_serviceOptions.SendMessagesToManager && _serviceOptions.SendMessagesPort > -1)
            {
                server = new ConnectionManagerServer(_logger, _serviceOptions.SendMessagesPort);
            }
            else
            {
                server = null;
            }

            _logger.LogInformation("STARTING CodeCompilerService...");
            server?.SendToClient("STARTING CodeCompilerService...");
            try
            {
                fileWatcher.Path = _codeCompilerLibOptions.InputPath;
                fileWatcher.Created += FileWatcher_FileCreated;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.InternalBufferSize = _serviceOptions.InternalBufferSize;
                _logger.LogInformation($"CodeCompilerService listening for files in directory {fileWatcher.Path}");
                server?.SendToClient($"CodeCompilerService listening for files in directory {fileWatcher.Path}");
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
                _logger.LogInformation($"File {e.Name} was added to input folder");
                server?.SendToClient($"File {e.Name} was added to input folder");
                var res = codeCompiler.CreateAssemblyToPath(e.FullPath, _codeCompilerLibOptions.OutputPath);
                if (res.Success)
                {
                    _logger.LogInformation($"File {e.Name} compiled correctly");
                    server?.SendToClient($"File {e.Name} compiled correctly");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in res.Diagnostics)
                    {
                        sb.Append(item.GetMessage());
                        sb.Append(Environment.NewLine);
                    }
                    _logger.LogWarning($"File {e.Name} compiled with errors: {sb.ToString()}");
                    server?.SendToClient($"File {e.Name} compiled with errors: {sb.ToString()}");
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
                _logger.LogInformation("ENDING CodeCompilerService");
                server?.SendToClient("ENDING CodeCompilerService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return base.StopAsync(cancellationToken);
        }
    }
}