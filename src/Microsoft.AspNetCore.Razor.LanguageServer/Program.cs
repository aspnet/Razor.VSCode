﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(1000);
                    }

                    break;
                }
            }

            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithMinimumLogLevel(LogLevel.Trace)
                    .WithHandler<RazorDocumentSynchronizationEndpoint>()
                    .WithHandler<RazorCompletionEndpoint>()
                    .WithHandler<RazorLanguageEndpoint>()
                    .WithHandler<RazorProjectEndpoint>()
                    .WithServices(services =>
                    {
                        services.AddSingleton<RemoteTextLoaderFactory, DefaultRemoteTextLoaderFactory>();
                        services.AddSingleton<VSCodeLogger, DefaultVSCodeLogger>();
                        services.AddSingleton<ProjectResolver, DefaultProjectResolver>();
                        services.AddSingleton<DocumentResolver, DefaultDocumentResolver>();
                        services.AddSingleton<FilePathNormalizer>();
                        services.AddSingleton<RazorProjectService, DefaultRazorProjectService>();
                        services.AddRazorShims();
                    }));

            await server.WaitForExit;
        }
    }
}
