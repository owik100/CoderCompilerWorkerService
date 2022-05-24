# Code Compiler Service
Windows service application whose purpose is to facilitate the compilation of a large number of small files. Service uses [Code compiler library](https://github.com/owik100/CodeCompilerLibrary).

 ## Prerequisites
[.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) can be very helpful to compile project.
<br/>
Additionally, you need to add a reference to the project [Code Compiler Settings Models](https://github.com/owik100/CodeCompilerSettingsModels)
```
├── Code Compiler Service 
└── Code Compiler Settings Models
```

 ## Installation
1. You can download app [here](https://github.com/owik100/CoderCompilerWorkerService/releases).
2. You should also set the input and output paths in json configuration file.


 ## Configuration
 Configuration is accessible via a appsettings.json file, but it is much easier to use a dedicated application - [Code Compiler Service Manager](https://github.com/owik100/CodeCompilerServiceManager).
 - Serilog section: All logging settings.
 - ServiceOptions: 
    - Interval: How often (ms) the service should check for files in the queue. 
    - MaxThreads: Number of threads to use for compilation.
    - InternalBufferSize: See [here](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=net-6.0).
    - SendMessagesToManager: Should the service send notifications to the manager.
    - SendMessagesToManagerAboutBeingAlive: Should the service send notifications about "being alive" to the manager.
    - SendMessagesPort: Port used for communication.
 - CodeCompilerLibOptions:
    - InputPath: Path to the folder where files for compilation will be added.
    - OutputPath: Path to the folder where the compiled files will appear.
    - BuildToConsoleApp: Specifies whether to compile files to the dll format or exe (Console app).
