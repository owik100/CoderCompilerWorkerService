using Basic.Reference.Assemblies;
using CodeCompilerNs;
using CodeCompilerSettingsModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Threading;

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
        Queue<FileSystemEventArgs> queueFiles;

        public Worker(ILogger<Worker> logger, WorkerServiceOptions serviceOptions, CodeCompilerLibOptions codeCompilerLibOptions)
        {
            _logger = logger;
            _serviceOptions = serviceOptions;
            _codeCompilerLibOptions = codeCompilerLibOptions;
            fileWatcher = new FileSystemWatcher();
            queueFiles = new Queue<FileSystemEventArgs>();

            OutputKind outputKind;
            if (_codeCompilerLibOptions.BuildType.Equals("ConsoleApplication", StringComparison.CurrentCultureIgnoreCase))
            {
                outputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
            }
            else
            {
                outputKind = Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
            }

            IEnumerable<PortableExecutableReference> referencesAssemby = ReferenceAssemblies.Net461;
            var netVersion = _codeCompilerLibOptions.NetVersionToCompile;
            switch (netVersion)
            {
                case "Net461":
                    referencesAssemby = ReferenceAssemblies.Net461;
                    break;
                case "Net472":
                    referencesAssemby = ReferenceAssemblies.Net472;
                    break;
                case "NetStandard13":
                    referencesAssemby = ReferenceAssemblies.NetStandard13;
                    break; 
                case "NetStandard20":
                    referencesAssemby = ReferenceAssemblies.NetStandard20;
                    break; 
                case "NetCoreApp31":
                    referencesAssemby = ReferenceAssemblies.NetCoreApp31;
                    break; 
                case "Net50":
                    referencesAssemby = ReferenceAssemblies.Net50;
                    break;
                case "Net60":
                    referencesAssemby = ReferenceAssemblies.Net60;
                    break;
                default:
                    referencesAssemby = ReferenceAssemblies.Net461;
                    break;
            }

            CSharpCompilationOptions compOptions = new CSharpCompilationOptions(outputKind)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release);

            codeCompiler = new CodeCompiler(referencesAssemby, compOptions);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!firstCall && _serviceOptions.SendMessagesToManagerAboutBeingAlive)
                    {
                        LogServerInfo("CodeCompilerService still alive...");
                    }

                    Semaphore sem = new Semaphore(_serviceOptions.MaxThreads, _serviceOptions.MaxThreads);
                    while (queueFiles.Count > 0)
                    {
                        sem.WaitOne();
                        var file = queueFiles.Dequeue();
                        ThreadPool.QueueUserWorkItem(ProcessItem, file);
                    }

                    int count = 0;
                    while (count < _serviceOptions.MaxThreads)
                    {
                        sem.WaitOne();
                        ++count;
                    }

                    void ProcessItem(object item)
                    {
                        FileSystemEventArgs paresedFile = item as FileSystemEventArgs;
                        var res = codeCompiler.CreateAssemblyToPath(paresedFile.FullPath, _codeCompilerLibOptions.OutputPath);
                        if (res.Success)
                        {
                            LogServerInfo($"File {paresedFile.Name} compiled correctly");
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var item2 in res.Diagnostics)
                            {
                                sb.Append(item2.GetMessage());
                                sb.Append(Environment.NewLine);
                            }
                            _logger.LogWarning($"File {paresedFile.Name} compiled with errors: {sb.ToString()}");
                            server?.SendToClient($"[SERVICE ERROR]File {paresedFile.Name} compiled with errors: {sb.ToString()}");
                        }
                        sem.Release();
                    }

                    firstCall = false;
                    await Task.Delay(_serviceOptions.Interval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                if(!stoppingToken.IsCancellationRequested && ex.Message != "A task was canceled.")
                {
                    _logger.LogError(ex.Message);
                }
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

            LogServerInfo("STARTING CodeCompilerService...");
            try
            {
                fileWatcher.Path = _codeCompilerLibOptions.InputPath;
                fileWatcher.Filter = "*.cs";
                fileWatcher.Created += FileWatcher_FileCreated;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.InternalBufferSize = _serviceOptions.InternalBufferSize;
                LogServerInfo($"CodeCompilerService listening for files in directory {fileWatcher.Path}");
            }
               
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogServerInfo("ENDING CodeCompilerService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return base.StopAsync(cancellationToken);
        }

        private void FileWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                LogServerInfo($"File {e.Name} was added to queue");
                queueFiles.Enqueue(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private void LogServerInfo(string message)
        {
            _logger.LogInformation(message);
            server?.SendToClient(message);
        }
    }
}