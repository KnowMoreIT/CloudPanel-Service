using CPService.Config;
using CPService.Tasks;
using log4net;
using Quartz;
using Quartz.Impl;
using System;
using System.ServiceProcess;

namespace CPService
{
    public partial class CPService : ServiceBase
    {
        private static IScheduler _scheduler;
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CPService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Retrieve the settings
            Config.ServiceSettings.ReadSettings();

            // Retrieve the intervals
            Config.ServiceTaskIntervals.GetIntervalsFromConfig();
            
            var scheduleFactory = new StdSchedulerFactory();
            _scheduler = scheduleFactory.GetScheduler();

            // Configure the jobs
            StartTasks();

            // Start the scheduler
            _scheduler.Start();
        }

        /// <summary>
        /// Start all schedules jobs
        /// </summary>
        private void StartTasks()
        {
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.ActiveDirectory.Get_DisabledUsersTask>().WithIdentity("Get_DisabledUsersTask").Build(),
                                   BuildTrigger("Get_DisabledUsersTask", ServiceTaskIntervals.ad_GetDisabledUsers));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.ActiveDirectory.Get_LockedUsersTask>().WithIdentity("Get_LockedUsersTask").Build(),
                                   BuildTrigger("Get_LockedUsersTask", ServiceTaskIntervals.ad_GetLockedUsers));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.Exchange.Get_MailboxSizesTask>().WithIdentity("Get_MailboxSizesTask").Build(),
                                   BuildTrigger("Get_MailboxSizesTask", ServiceTaskIntervals.exch_GetMailboxSizes));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.Exchange.Get_MailboxDatabaseSizesTask>().WithIdentity("Get_MailboxDatabaseSizesTask").Build(),
                                   BuildTrigger("Get_MailboxDatabaseSizesTask", ServiceTaskIntervals.exch_GetMailboxDatabaseSizes));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.Exchange.Get_MessageTrackingLogsTask>().WithIdentity("Get_MessageTrackingLogsTask").Build(),
                                   BuildTrigger("Get_MessageTrackingLogsTask", ServiceTaskIntervals.exch_GetMessageTrackingLogs));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.Database.FindMissingDataTask>().WithIdentity("FindMissingDataTask").Build(),
                                   BuildTrigger("FindMissingDataTask", ServiceTaskIntervals.db_FindMissingData));
            
            _scheduler.ScheduleJob(JobBuilder.Create<Tasks.Database.UpdateDatabaseHistoryTask>().WithIdentity("UpdateDatabaseHistoryTask").Build(),
                                   BuildTrigger("UpdateDatabaseHistoryTask", ServiceTaskIntervals.db_UpdateDatabaseHistory));
        }

        /// <summary>
        /// Build the trigger for the job
        /// </summary>
        /// <param name="name"></param>
        /// <param name="minutes"></param>
        /// <returns></returns>
        private ITrigger BuildTrigger(string name, int minutes)
        {
            logger.InfoFormat("Building trigger {0} with {1} minute(s)", name, minutes);
            return TriggerBuilder.Create()
                                 .WithIdentity(name)
                                 .StartAt(DateTime.Now.AddMinutes(minutes))
                                 .WithSimpleSchedule(x => x.WithIntervalInMinutes(minutes).RepeatForever())
                                 .Build();
        }

        public static void LogError(string message)
        {
            logger.ErrorFormat(message);
        }

        protected override void OnStop()
        {
            if (_scheduler != null)
                _scheduler.Shutdown(true);
        }
    }
}
