using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Activities
{
    public class ClearShiftsActivity
    {
        private readonly ClearScheduleOptions _options;
        private readonly IScheduleDestinationService _scheduleDestinationService;

        public ClearShiftsActivity(ClearScheduleOptions options, IScheduleDestinationService scheduleDestinationService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
        }

        [FunctionName(nameof(ClearShiftsActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] ClearScheduleModel clearScheduleModel, ILogger log)
        {
            var resultModel = new ResultModel();

            try
            {
                var batchSize = clearScheduleModel.QueryEndDate.HasValue ? _options.ClearScheduleMaxBatchSize : _options.ClearScheduleBatchSize;

                var shifts = await _scheduleDestinationService.ListShiftsAsync(clearScheduleModel.TeamId, clearScheduleModel.StartDate, clearScheduleModel.QueryEndDate ?? clearScheduleModel.EndDate, batchSize);

                // restrict the shifts to delete to those that actually started between the start and end dates
                shifts = shifts.Where(s => s.StartDate < clearScheduleModel.EndDate).ToList();
                if (shifts.Count > 0)
                {
                    var tasks = shifts
                        .Select(shift => TryDeleteShiftAsync(clearScheduleModel, shift, log))
                        .ToArray();

                    var result = await Task.WhenAll(tasks);
                    resultModel.DeletedCount = result.Count(r => r == true);
                }

                resultModel.Finished = shifts.Count == 0;
            }
            catch (Exception ex)
            {
                log.LogShiftError(ex, clearScheduleModel, nameof(_scheduleDestinationService.ListShiftsAsync));
            }

            return resultModel;
        }

        private async Task<bool> TryDeleteShiftAsync(ClearScheduleModel clearScheduleModel, ShiftModel shift, ILogger log)
        {
            try
            {
                await _scheduleDestinationService.DeleteShiftAsync(clearScheduleModel.TeamId, shift);
                return true;
            }
            catch (Exception ex)
            {
                log.LogShiftError(ex, clearScheduleModel, nameof(_scheduleDestinationService.DeleteShiftAsync), shift);
                return false;
            }
        }
    }
}
