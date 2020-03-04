// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration
{
    using System.IO;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// The main entry point for program/application execution.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Driver method.
        /// </summary>
        /// <param name="args">Project arguments or pre-defined arguments for the project.</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates an environment for this web application to execute successfully.
        /// </summary>
        /// <param name="args">Project arguments or command line arguments.</param>
        /// <returns>A builder for <see cref="IWebHost"/>.</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
    }
}