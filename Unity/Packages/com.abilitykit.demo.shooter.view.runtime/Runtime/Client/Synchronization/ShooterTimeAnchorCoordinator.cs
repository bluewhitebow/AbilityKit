#nullable enable

using System;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterTimeAnchorCoordinator
    {
        private SyncClock _localClock;

        public ShooterTimeAnchorCoordinator(int tickRate)
        {
            _localClock = CreateClock(tickRate);
        }

        public SyncTimeAnchor LastLocalAnchor { get; private set; }

        public SyncTimeAnchor AdvanceLocal()
        {
            LastLocalAnchor = _localClock.Advance();
            return LastLocalAnchor;
        }

        public void Reset(int tickRate)
        {
            _localClock = CreateClock(tickRate);
            LastLocalAnchor = default;
        }

        public static ShooterTimeAnchorCoordinator CreateLocal(int tickRate)
        {
            return new ShooterTimeAnchorCoordinator(tickRate);
        }

        public static ShooterRemoteTimeAnchorProjection ProjectRemote(
            in ShooterGatewayWorldStartAnchor worldStartAnchor,
            long serverNowTicks)
        {
            if (!worldStartAnchor.IsValid || serverNowTicks <= 0L)
            {
                return default;
            }

            var catchUp = WorldStartFrameCatchUpCalculator.Calculate(worldStartAnchor.ToFrameStartAnchor(), serverNowTicks);
            if (!catchUp.AnchorValid)
            {
                return default;
            }

            var timelineTicks = Math.Max(0, catchUp.TargetFrame - worldStartAnchor.StartFrame);
            var anchor = SyncTimeAnchor
                .FromLocalFrame(catchUp.TargetFrame, timelineTicks, catchUp.ElapsedSeconds)
                .WithAuthoritativeFrame(catchUp.TargetFrame)
                .WithServerTicks(serverNowTicks);

            return new ShooterRemoteTimeAnchorProjection(
                true,
                serverNowTicks,
                catchUp.TargetFrame,
                catchUp.CatchUpFrames,
                catchUp.ElapsedSeconds,
                anchor);
        }

        private static SyncClock CreateClock(int tickRate)
        {
            if (tickRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            }

            return new SyncClock(1d / tickRate, timelineTicksPerStep: 1L);
        }
    }

    public readonly struct ShooterRemoteTimeAnchorProjection
    {
        public ShooterRemoteTimeAnchorProjection(
            bool anchorValid,
            long serverNowTicks,
            int targetFrame,
            int catchUpFrames,
            double elapsedSeconds,
            SyncTimeAnchor timeAnchor)
        {
            AnchorValid = anchorValid;
            ServerNowTicks = serverNowTicks;
            TargetFrame = targetFrame;
            CatchUpFrames = catchUpFrames;
            ElapsedSeconds = elapsedSeconds;
            TimeAnchor = timeAnchor;
        }

        public bool AnchorValid { get; }
        public long ServerNowTicks { get; }
        public int TargetFrame { get; }
        public int CatchUpFrames { get; }
        public double ElapsedSeconds { get; }
        public SyncTimeAnchor TimeAnchor { get; }
    }
}
