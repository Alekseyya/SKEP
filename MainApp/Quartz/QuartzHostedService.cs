using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Spi;


namespace MainApp.Quartz
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly TimesheetConfig _timesheetConfig;
        private readonly ADConfig _adConfig;
        private readonly BitrixConfig _bitrixConfig;


        public IScheduler Scheduler { get; set; }

        public QuartzHostedService(
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory, IOptions<ADConfig> adOptions, IOptions<BitrixConfig> bitrixOptions, IOptions<TimesheetConfig> timesheetOptions)
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
            _timesheetConfig = timesheetOptions.Value ?? throw new ArgumentNullException(nameof(timesheetOptions));
            _adConfig = adOptions.Value ?? throw new ArgumentNullException(nameof(adOptions));
            _bitrixConfig = bitrixOptions.Value ?? throw new ArgumentNullException(nameof(bitrixOptions));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            IJobDetail job = null;
            ITrigger trigger = null;

            if (_adConfig.SyncIntervalEnabled && !string.IsNullOrEmpty(_adConfig.SyncIntervalValue))
            {
                job = CreateJob(typeof(ADSyncJob), "ActiveDirectorySyncJob", "syncJob");
                trigger = ActiveDirectoryDailyTrigger();
                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }

            if (_bitrixConfig.SyncIntervalEnabled && !string.IsNullOrEmpty(_bitrixConfig.SyncIntervalValue))
            {
                job = CreateJob(typeof(BitrixSyncJob), "BitrixSyncJob", "syncJob");
                trigger = BitrixMinutesTrigger();
                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }

            if (_timesheetConfig.ProcessingIntervalEnabled && !string.IsNullOrEmpty(_timesheetConfig.ProcessingIntervalValue))
            {
                job = CreateJob(typeof(TimesheetJob), "TimesheetJob", "syncJob");
                trigger = TimesheetDailyTrigger();
                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }
            await Scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
        }

        private ITrigger ActiveDirectoryDailyTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity("ActiveDirectorySyncCron")
                .StartNow()
                .WithDailyTimeIntervalSchedule(x => x
                    .WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(_adConfig.Hours, _adConfig.Minutes)))
                .Build();
        }

        private ITrigger TimesheetDailyTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity("TimesheetCron")
                .WithDailyTimeIntervalSchedule(x => x
                    .WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(_timesheetConfig.Hours, _timesheetConfig.Minutes)))
                .Build();
        }

        private ITrigger BitrixMinutesTrigger()
        {
            return TriggerBuilder.Create()
                .WithIdentity("BitrixCron")
                .StartNow()
                .WithSimpleSchedule(x =>
                    x.WithIntervalInMinutes(_bitrixConfig.Minutes)
                    .RepeatForever())
                .Build();
        }

        private static IJobDetail CreateJob(Type job, string syncJob, string syncGroup)
        {
            return JobBuilder
                .Create(job)
                .WithIdentity(syncJob,syncGroup)
                .Build();
        }
    }
}
