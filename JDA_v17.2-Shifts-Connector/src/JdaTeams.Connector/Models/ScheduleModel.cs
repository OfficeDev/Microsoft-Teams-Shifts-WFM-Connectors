﻿using System;

namespace JdaTeams.Connector.Models
{
    public class ScheduleModel
    {
        public bool IsEnabled { get; set; }
        public string Status { get; set; }
        public string TimeZone { get; set; }

        public bool IsProvisioned => Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true 
            && IsEnabled;

        public bool IsUnavailable => Status?.Equals("NotStarted", StringComparison.OrdinalIgnoreCase) == true
            || Status?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true;

        public static ScheduleModel Create(string TimeZone)
        {
            return new ScheduleModel
            {
                TimeZone = TimeZone ?? throw new ArgumentNullException(nameof(TimeZone)),
                IsEnabled = true
            };
        }
    }
}
