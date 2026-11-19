namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    // Choice/validation model from Development18675d6 HolonPurgeModels.cs.
    // This type is the single owner when the remaining purge module is integrated.
    public class PurgeChoice
    {
        public bool? Enabled { get; set; }
        public int? Days { get; set; }
        public bool IsEmpty => !Enabled.HasValue && !Days.HasValue;
        public const int MaxDays = 3650;
        public static string Validate(int? days) => days.HasValue && (days.Value < 0 || days.Value > MaxDays) ? "Days must be a whole number from 0 to " + MaxDays + " (0 purges at deletion)." : null;
    }
}
