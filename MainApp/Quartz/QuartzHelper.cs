using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace MainApp.Quartz
{
    public class QuartzHelper
    {
        private static IScheduler _scheduler;
        public static void Configure(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }
        public static DateTime GetNextFireTimeForJob(string jobName, string groupName = "")
        {
            JobKey jobKey = new JobKey(jobName, groupName);
            DateTime nextFireTime = DateTime.MinValue;

            if (_scheduler != null)
            {
                Task<bool> isJobExisting = _scheduler.CheckExists(jobKey);
                if (isJobExisting.Result)
                {
                    var detail = _scheduler.GetJobDetail(jobKey);
                    var triggers = _scheduler.GetTriggersOfJob(jobKey);

                    if (triggers.Result.Count > 0)
                    {
                        var nextFireTimeUtc = triggers.Result.FirstOrDefault().GetNextFireTimeUtc();
                        if (nextFireTimeUtc != null
                            && nextFireTimeUtc.HasValue == true)
                        {
                            nextFireTime = TimeZone.CurrentTimeZone.ToLocalTime(nextFireTimeUtc.Value.DateTime);
                        }
                    }
                }
            }

            return (nextFireTime);
        }

        public static DateTime GetPreviousFireTimeForJob(string jobName, string groupName = "")
        {
            JobKey jobKey = new JobKey(jobName, groupName);
            DateTime previousFireTime = DateTime.MinValue;

            if (_scheduler != null)
            {
                Task<bool> isJobExisting = _scheduler.CheckExists(jobKey);
                if (isJobExisting.Result)
                {
                    var detail = _scheduler.GetJobDetail(jobKey);
                    var triggers = _scheduler.GetTriggersOfJob(jobKey);

                    if (triggers.Result.Count > 0)
                    {
                        var previousFireTimeUtc = triggers.Result.FirstOrDefault().GetPreviousFireTimeUtc();
                        if (previousFireTimeUtc != null
                            && previousFireTimeUtc.HasValue == true)
                        {
                            previousFireTime = TimeZone.CurrentTimeZone.ToLocalTime(previousFireTimeUtc.Value.DateTime);
                        }
                    }
                }
            }

            return (previousFireTime);
        }

        public static string GetStatusInfoForJob(string jobName, string groupName = "")
        {
            JobKey jobKey = new JobKey(jobName, groupName);
            string status = "Ожидание";

            if (_scheduler != null)
            {
                Task<IReadOnlyCollection<IJobExecutionContext>> executingJobs = _scheduler.GetCurrentlyExecutingJobs();

                foreach (IJobExecutionContext j in executingJobs.Result)
                {
                    if (j.JobDetail.Key.Equals(jobKey) == true)
                    {
                        status = "Выполняется";
                    }
                }
            }

            return status;
        }
    }
}