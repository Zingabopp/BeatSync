using BeatSyncLib.Downloader.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSyncLib.Downloader
{
    public struct JobStats
    {
        public int Jobs;
        public int FailedJobs;
        public int SkippedJobs;

        public JobStats(int jobs, int failedJobs, int skippedJobs)
        {
            Jobs = jobs;
            FailedJobs = failedJobs;
            SkippedJobs = skippedJobs;
        }

        public JobStats(IEnumerable<JobResult> jobResults)
        {
            Jobs = 0;
            FailedJobs = 0;
            SkippedJobs = 0;
            foreach (var result in jobResults)
            {
                Jobs++;
                if (!result.Successful)
                    FailedJobs++;
                else if (result.TargetResults.All(t => t.SongState != SongState.Wanted))
                    SkippedJobs++;
            }
        }

        public override string ToString()
        {
            if (SkippedJobs > 0)
                return $"{Jobs - FailedJobs}/{Jobs}, {SkippedJobs} skipped";
            else
                return $"{Jobs - FailedJobs}/{Jobs}";
        }

        public static JobStats operator -(JobStats a) => new JobStats(-a.Jobs, -a.FailedJobs, -a.SkippedJobs);
        public static JobStats operator +(JobStats a, JobStats b) =>
            new JobStats(a.Jobs + b.Jobs, a.FailedJobs + b.FailedJobs, a.SkippedJobs + b.SkippedJobs);
        public static JobStats operator -(JobStats a, JobStats b) => a + (-b);

    }
}
