﻿using BitShelter.Data;
using BitShelter.Models;
using BitShelter.Service.Jobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShelter.Service.Scheduler
{
  public static class VssScheduler
  {
    public static IJobDetail SnapshotJob => JobBuilder.Create<SnapshotJob>()
          .WithIdentity("SnapshotJob")
          .Build();
    public static IJobDetail BackupJob => JobBuilder.Create<BackupJob>()
          .WithIdentity("BackupJob")
          .Build();
    public static IJobDetail PruneJob => JobBuilder.Create<PruneJob>()
          .WithIdentity("PruneJob")
          .Build();

    public static void CreateAllTriggers(IEnumerable<SnapshotRule> rules)
    {
      QuartzScheduler.Instance.Scheduler.Clear().Wait();

      CreatePruneJobTrigger();
      CreateSnapshotJobTriggers(rules);
    }

    private static void CreatePruneJobTrigger()
    {
      ITrigger pruneTrigger = TriggerBuilder.Create()
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(1).RepeatForever())
        .Build();

      QuartzScheduler.Instance.Scheduler.ScheduleJob(PruneJob, pruneTrigger).Wait();
    }

    private static void CreateSnapshotJobTriggers(IEnumerable<SnapshotRule> rules)
    {
      List<ITrigger> snapshotTriggers = new List<ITrigger>();

      foreach (var rule in rules.Where(r => r.Enabled))
      {
        if (rule.HasCalendar())
          QuartzScheduler.Instance.Scheduler.AddCalendar(rule.GetCalendarName(), rule.GetCalendar(), true, true).Wait();

        snapshotTriggers.Add(rule.GetTrigger());
      }

      Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>> newSchedules = new Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>>()
      {
        { SnapshotJob, snapshotTriggers }
      };

      QuartzScheduler.Instance.Scheduler.ScheduleJobs(newSchedules, true).Wait();
    }
  }
}
