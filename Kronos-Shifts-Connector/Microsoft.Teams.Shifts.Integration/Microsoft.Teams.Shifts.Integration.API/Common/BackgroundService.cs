// <copyright file="BackgroundService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// BackgroundService class that inherits IHostedService and implements the methods related to background tasks.
    /// </summary>
    public class BackgroundService : IHostedService
    {
        private readonly BackgroundTaskWrapper taskWrapper;
        private readonly TelemetryClient telemetryClient;
        private CancellationTokenSource tokenSource;
        private Task currentTask;
        private CultureInfo cultureInfoProvider = CultureInfo.InvariantCulture;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundService"/> class.
        /// </summary>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public BackgroundService(BackgroundTaskWrapper taskWrapper, TelemetryClient telemetryClient)
        {
            this.taskWrapper = taskWrapper;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Method to start the background task when application starts.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task instance.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"BackgroundService StartAsync method started");

            // Creating a linked token so that we can trigger cancellation outside of this token's cancellation
            this.tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    this.telemetryClient.TrackTrace("BackgroundService Dequeue method started.");

                    // Dequeuing a task and running it in background until the cancellation is triggered or task is completed
                    this.currentTask = this.taskWrapper.Dequeue(this.tokenSource.Token);
                    await this.currentTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    // Execution has been cancelled.
                    this.telemetryClient.TrackException(exception);
                }
            }
        }

        /// <summary>
        /// Triggered when the host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation to the executing method.</param>
        /// <returns>A task instance.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace("BackgroundService StopAsync method started");

            // Signal cancellation to the executing method
            this.tokenSource.Cancel();

            // If Stop called without start
            if (this.currentTask == null)
            {
                return;
            }

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(this.currentTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
        }
    }
}