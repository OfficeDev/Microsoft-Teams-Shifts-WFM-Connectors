// ---------------------------------------------------------------------------
// <copyright file="OrchestratorTimerTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class OrchestratorTimerTrigger
    {
        private readonly FeatureOptions _featureOptions;
        private readonly TeamOrchestratorOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly ISystemTimeService _systemTimeService;

        public OrchestratorTimerTrigger(TeamOrchestratorOptions teamOrchestratorOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, ISystemTimeService systemTimeService)
        {
            _options = teamOrchestratorOptions ?? throw new ArgumentNullException(nameof(teamOrchestratorOptions));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _systemTimeService = systemTimeService ?? throw new ArgumentNullException(nameof(systemTimeService));
        }

        [FunctionName(nameof(OrchestratorTimerTrigger))]
        public async Task Run([TimerTrigger("%OrchestratorScheduleExpression%")] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            if (_options.SuspendAllSyncs)
            {
                log.LogOrchestratorTimerTriggerSuspended();
                return;
            }

            var sw = Stopwatch.StartNew();
            var executionStage = string.Empty;

            try
            {
                executionStage = nameof(_scheduleConnectorService.ListConnectionsAsync);
                var connections = await _scheduleConnectorService.ListConnectionsAsync().ConfigureAwait(false);
                var enabledConnections = connections.Where(c => c.Enabled).ToList();
                log.LogOrchestratorTimerTriggerConnectionsCount(connections.Count(), enabledConnections.Count);

                // the other orchestrators depend on the cache being populated with employee data so
                // do this one first
                executionStage = $"Starting {nameof(EmployeeCacheOrchestrator)}...";
                await StartOrchestratorsAsync(nameof(EmployeeCacheOrchestrator), EmployeeCacheOrchestrator.InstanceIdPattern, enabledConnections, _options.EmployeeCacheFrequencyMinutes, starter, log).ConfigureAwait(false);

                // and the rest in parallel
                executionStage = "Starting other orchestrators in parallel...";
                var orchestrationTasks = new List<Task>();
                if (_featureOptions.EnableShiftSync)
                {
                    orchestrationTasks.Add(StartOrchestratorsAsync(nameof(ShiftsOrchestrator), ShiftsOrchestrator.InstanceIdPattern, enabledConnections, _options.ShiftsFrequencyMinutes, starter, log));
                }

                if (_featureOptions.EnableOpenShiftSync)
                {
                    orchestrationTasks.Add(StartOrchestratorsAsync(nameof(OpenShiftsOrchestrator), OpenShiftsOrchestrator.InstanceIdPattern, enabledConnections, _options.OpenShiftsFrequencyMinutes, starter, log));
                }

                if (_featureOptions.EnableTimeOffSync)
                {
                    orchestrationTasks.Add(StartOrchestratorsAsync(nameof(TimeOffOrchestrator), TimeOffOrchestrator.InstanceIdPattern, enabledConnections, _options.TimeOffFrequencyMinutes, starter, log));
                }

                if (_featureOptions.EnableAvailabilitySync)
                {
                    orchestrationTasks.Add(StartOrchestratorsAsync(nameof(AvailabilityOrchestrator), AvailabilityOrchestrator.InstanceIdPattern, enabledConnections, _options.AvailabilityFrequencyMinutes, starter, log));
                }

                if (_featureOptions.EnableEmployeeTokenRefresh)
                {
                    orchestrationTasks.Add(StartOrchestratorsAsync(nameof(EmployeeTokenRefreshOrchestrator), EmployeeTokenRefreshOrchestrator.InstanceIdPattern, enabledConnections, _options.EmployeeTokenRefreshFrequencyMinutes, starter, log));
                }

                await Task.WhenAll(orchestrationTasks).ConfigureAwait(false);

                // now that we have dealt with all the orchestrators for the teams, we need to
                // update the last execution times against the team connections that were actually updated
                executionStage = "Updating last execution dates...";
                var updatedConnections = enabledConnections.Where(c => c.Updated);
                await Task.WhenAll(updatedConnections
                    .Select(c => _scheduleConnectorService.UpdateLastExecutionDatesAsync(c))).ConfigureAwait(false);

                sw.Stop();
                log.LogOrchestratorTimerTriggerSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                log.LogOrchestratorTimerTriggerError(ex, executionStage, sw.ElapsedMilliseconds);
            }
        }

        private DateTime GetLastExecutionTime(ConnectionModel c, string orchestratorName)
        {
            return orchestratorName switch
            {
                nameof(AvailabilityOrchestrator) => c.LastAOExecution ?? DateTime.MinValue,
                nameof(EmployeeCacheOrchestrator) => c.LastECOExecution ?? DateTime.MinValue,
                nameof(EmployeeTokenRefreshOrchestrator) => c.LastETROExecution ?? DateTime.MinValue,
                nameof(OpenShiftsOrchestrator) => c.LastOSOExecution ?? DateTime.MinValue,
                nameof(ShiftsOrchestrator) => c.LastSOExecution ?? DateTime.MinValue,
                nameof(TimeOffOrchestrator) => c.LastTOOExecution ?? DateTime.MinValue,
                _ => _systemTimeService.UtcNow,
            };
        }

        private void SetLastExecutionTime(ConnectionModel connection, string orchestratorName, DateTime utcNow)
        {
            switch (orchestratorName)
            {
                case nameof(AvailabilityOrchestrator):
                {
                    connection.LastAOExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
                case nameof(EmployeeCacheOrchestrator):
                {
                    connection.LastECOExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
                case nameof(EmployeeTokenRefreshOrchestrator):
                {
                    connection.LastETROExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
                case nameof(OpenShiftsOrchestrator):
                {
                    connection.LastOSOExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
                case nameof(ShiftsOrchestrator):
                {
                    connection.LastSOExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
                case nameof(TimeOffOrchestrator):
                {
                    connection.LastTOOExecution = utcNow;
                    connection.Updated = true;
                    break;
                }
            }
        }

        private async Task StartOrchestratorAndUpdateLastExecution(string orchestratorName, IDurableOrchestrationClient starter, ConnectionModel connection, TeamModel teamModel, string instanceId, ILogger log)
        {
            await starter.StartNewAsync(orchestratorName, instanceId, teamModel).ConfigureAwait(false);
            log.LogStartOrchestrator(orchestratorName, teamModel.TeamId);

            SetLastExecutionTime(connection, orchestratorName, _systemTimeService.UtcNow);
        }

        private async Task StartOrchestratorsAsync(string orchestratorName, string instancePattern, IEnumerable<ConnectionModel> connections, int frequencyMinutes, IDurableOrchestrationClient starter, ILogger log)
        {
            if (frequencyMinutes < 1)
                frequencyMinutes = 1;

            var pendingTime = _systemTimeService.UtcNow.AddMinutes(-frequencyMinutes);
            var hungThresholdTime = _systemTimeService.UtcNow.AddMinutes(-_options.OrchestratorHungThresholdMinutes);

            var pendingConnections = connections
                .Where(c => GetLastExecutionTime(c, orchestratorName) <= pendingTime)
                .OrderBy(c => GetLastExecutionTime(c, orchestratorName))
                .ToList();
            if (pendingConnections.Count == 0)
            {
                // there is nothing to do for this orchestrator this iteration
                return;
            }

            var skip = 0;
            var take = connections.Count() / frequencyMinutes > 0 ? connections.Count() / frequencyMinutes : 1;

            do
            {
                log.LogPendingOrchestratorsDetail(orchestratorName, pendingConnections.Count, skip, take);

                int notStarted = 0;
                foreach (var connection in pendingConnections.Skip(skip).Take(take))
                {
                    // get the current status of the orchestrator
                    var teamModel = TeamModel.FromConnection(connection);
                    var instanceId = string.Format(instancePattern, teamModel.TeamId);

                    var status = await starter.GetStatusAsync(instanceId).ConfigureAwait(false);
                    if (status != null)
                    {
                        switch (status.RuntimeStatus)
                        {
                            case OrchestrationRuntimeStatus.Running:
                            {
                                // the orchestrator is still running since the last time we started
                                // it, so either it genuinely has not completed yet, or it has hung.
                                // We will assume it has hung if it's last updated time is less than
                                // the hung threshold time
                                if (status.LastUpdatedTime < hungThresholdTime)
                                {
                                    // it appears to have hung, so force terminate it and try to
                                    // start it up again in the next iteration
                                    await starter.TerminateAsync(instanceId, "Stalled").ConfigureAwait(false);
                                    log.LogStalledOrchestrator(orchestratorName, status.LastUpdatedTime, hungThresholdTime, teamModel.TeamId);
                                }
                                notStarted++;
                                break;
                            }
                            case OrchestrationRuntimeStatus.Pending:
                            case OrchestrationRuntimeStatus.Unknown:
                            {
                                // either it has not managed to start in the duration of a single
                                // iteration or we don't know what state the orchestrator is in, in
                                // either case log the fact when the duration exceeds the
                                // hungThresholdTime and use an alert to notify that a manual
                                // intervention will be required.
                                // Note: previously we attempted to terminate orchestrators in this
                                // state but a bug in the durabletask framework (2.2.2) means that
                                // doing so locks them in the Pending state permanently
                                if (status.LastUpdatedTime < hungThresholdTime)
                                {
                                    if (status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                                    {
                                        log.LogPendingOrchestrator(orchestratorName, status.LastUpdatedTime, teamModel.TeamId);
                                    }
                                    else
                                    {
                                        log.LogUnknownStatusOrchestrator(orchestratorName, status.LastUpdatedTime, teamModel.TeamId);
                                    }
                                }
                                notStarted++;
                                break;
                            }
                            case OrchestrationRuntimeStatus.Completed:
                            case OrchestrationRuntimeStatus.Failed:
                            case OrchestrationRuntimeStatus.Canceled:
                            case OrchestrationRuntimeStatus.Terminated:
                            {
                                await StartOrchestratorAndUpdateLastExecution(orchestratorName, starter, connection, teamModel, instanceId, log).ConfigureAwait(false);
                                break;
                            }
                            default:
                            {
                                notStarted++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // no status means that it has never run, so start it for the first time now
                        await StartOrchestratorAndUpdateLastExecution(orchestratorName, starter, connection, teamModel, instanceId, log).ConfigureAwait(false);
                    }
                }

                // if we failed to start any of the pending connections that we took then we should
                // skip this batch and take another batch for the failures - repeat this until there
                // are no more pending connections
                skip += take;
                take = notStarted;
            } while (take > 0 && skip < pendingConnections.Count);
        }
    }
}
