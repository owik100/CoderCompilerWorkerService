using CodeCompilerNs;
using CodeCompilerSettingsModels;
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

                    //CreateDummyFiles();

                    Semaphore sem = new Semaphore(_serviceOptions.MaxThreads, _serviceOptions.MaxThreads);
                    while (queueFiles.Count > 0)
                    {
                        sem.WaitOne();
                        var file = queueFiles.Dequeue();
                        ThreadPool.QueueUserWorkItem(ProcessItem, file);
                    }

                    // When the queue is empty, you have to wait for all processing
                    // threads to complete.
                    // If you can acquire the semaphore MaxThreads times, all workers are done
                    int count = 0;
                    while (count < _serviceOptions.MaxThreads)
                    {
                        sem.WaitOne();
                        ++count;
                    }

                    // the code to process an item
                    void ProcessItem(object item)
                    {
                        FileSystemEventArgs paresedFile = item as FileSystemEventArgs;
                        var res = codeCompiler.CreateAssemblyToPath(paresedFile.FullPath, _codeCompilerLibOptions.OutputPath);
                        if (res.Success)
                        {
                            _logger.LogInformation($"File {paresedFile.Name} compiled correctly");
                            server?.SendToClient($"File {paresedFile.Name} compiled correctly");
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
                            server?.SendToClient($"File {paresedFile.Name} compiled with errors: {sb.ToString()}");
                        }
                        // when done processing, release the semaphore
                        sem.Release();
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
                _logger.LogInformation($"File {e.Name} was added to queue");
                server?.SendToClient($"File {e.Name} was added to queue");
                queueFiles.Enqueue(e);
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

        private void CreateDummyFiles()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (StreamWriter sw = File.CreateText(fileWatcher.Path + "\\File" + i + ".cs"))
                {
                    sw.WriteLine("namespace HelloWorld{class Hello{static void Main(string[] args){System.Console.WriteLine(\"Hello World!\");System.Threading.Thread.Sleep(3000);System.Console.ReadKey();}}}");
                }
            }
        }
    }
}