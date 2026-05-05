using System;

public struct RelativeTimeInfo
{
    public string Value; // Le chiffre (ex: "30") ou vide
    public string Token; // La clé pure (ex: "#time.minutes_ago")
}

public static class TimeFormatter
{
    public static RelativeTimeInfo GetRelativeTime( DateTime pastDate )
    {
        TimeSpan timeSince = DateTime.UtcNow - pastDate.ToUniversalTime();

        if ( timeSince.TotalMinutes < 1 )
            return new RelativeTimeInfo { Value = "", Token = "#common.time.just_now" };

        if ( timeSince.TotalHours < 1 )
            return new RelativeTimeInfo { Value = timeSince.Minutes.ToString(), Token = "#common.time.minutes_ago" };

        if ( timeSince.TotalDays < 1 )
            return new RelativeTimeInfo { Value = timeSince.Hours.ToString(), Token = "#common.time.hours_ago" };

        if ( timeSince.TotalDays < 2 )
            return new RelativeTimeInfo { Value = "", Token = "#common.time.yesterday" };

        if ( timeSince.TotalDays < 7 )
            return new RelativeTimeInfo { Value = timeSince.Days.ToString(), Token = "#common.time.days_ago" };

        if ( timeSince.TotalDays < 30 )
            return new RelativeTimeInfo { Value = ((int)(timeSince.TotalDays / 7)).ToString(), Token = "#common.time.weeks_ago" };

        if ( timeSince.TotalDays < 365 )
            return new RelativeTimeInfo { Value = ((int)(timeSince.TotalDays / 30)).ToString(), Token = "#common.time.months_ago" };

        return new RelativeTimeInfo { Value = ((int)(timeSince.TotalDays / 365)).ToString(), Token = "#common.time.years_ago" };
    }
}