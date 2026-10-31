using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Idle/error polling policy: 1, 2, 4, 8, 16, then 30 seconds; useful work resets it.</summary>
    public sealed class HyperDriveWorkerBackoff
    {
        private int _idleCount;

        public TimeSpan NextDelay(bool madeProgress)
        {
            if (madeProgress)
            {
                _idleCount = 0;
                return TimeSpan.Zero;
            }
            int seconds = Math.Min(30, 1 << Math.Min(_idleCount, 5));
            _idleCount = Math.Min(_idleCount + 1, 5);
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
