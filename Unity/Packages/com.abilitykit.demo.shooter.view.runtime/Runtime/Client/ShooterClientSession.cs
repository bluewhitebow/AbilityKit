#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientSession
    {
        private readonly ShooterPresentationSessionContext _presentationSession;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly ShooterClientInputCoordinator _input;

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate)
            : this(runtime, presentation, tickRate, (ShooterGatewaySnapshotDecoder?)null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
            : this(runtime, presentation, tickRate, decoder, null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder, IShooterRoomGatewayClient? gateway)
            : this(runtime, ShooterPresentationSessionContext.CreateFromFacade(presentation), tickRate, decoder, gateway)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationSessionContext presentationSession, int tickRate)
            : this(runtime, presentationSession, tickRate, (ShooterGatewaySnapshotDecoder?)null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationSessionContext presentationSession, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
            : this(runtime, presentationSession, tickRate, decoder, null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationSessionContext presentationSession, int tickRate, ShooterGatewaySnapshotDecoder? decoder, IShooterRoomGatewayClient? gateway)
        {
            _presentationSession = presentationSession ?? throw new ArgumentNullException(nameof(presentationSession));
            _presentation = _presentationSession.Presentation;
            _frameSync = new ShooterClientFrameSyncCoordinator(runtime, _presentation, tickRate, decoder);
            _input = new ShooterClientInputCoordinator(_frameSync, gateway);
        }

        public bool IsStarted => _frameSync.IsStarted;

        public int CurrentFrame => _frameSync.CurrentFrame;

        public ShooterPresentationSessionContext PresentationSession => _presentationSession;

        public ShooterPresentationFacade Presentation => _presentation;

        public ShooterClientFrameSyncController FrameSync => _frameSync.Controller;

        public ShooterClientFrameSyncCoordinator FrameSyncCoordinator => _frameSync;

        public ShooterClientInputCoordinator InputCoordinator => _input;

        public ShooterClientReconciliationResult LastReconciliationResult => _frameSync.LastReconciliationResult;

        public bool NeedsFullSnapshotResync => _frameSync.NeedsFullSnapshotResync;

        public ShooterClientRecoveryState RecoveryState => _frameSync.RecoveryState;

        public ShooterClientResyncReason LastResyncReason => _frameSync.LastResyncReason;

        public int LastResyncClientFrame => _frameSync.LastResyncClientFrame;

        public int LastResyncAuthoritativeFrame => _frameSync.LastResyncAuthoritativeFrame;

        public uint LastResyncClientStateHash => _frameSync.LastResyncClientStateHash;

        public uint LastResyncAuthoritativeStateHash => _frameSync.LastResyncAuthoritativeStateHash;

        public bool HasGateway => _input.HasGateway;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _frameSync.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            return _input.SubmitLocalInput(playerId, moveX, moveY, aimX, aimY, fire);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return _input.SubmitLocalInput(in command);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _input.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            return _frameSync.Tick(deltaTime);
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _frameSync.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _frameSync.TryEnterCatchUp(authoritativeFrame);
        }

        public Task<ShooterGatewayFullStateSyncRequestResult> RequestFullSnapshotResyncAsync(
            IShooterRoomGatewayRoomClient roomClient,
            ShooterGatewayFullStateSyncRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (roomClient == null) throw new ArgumentNullException(nameof(roomClient));
            return roomClient.RequestFullStateSyncAsync(request, timeout, cancellationToken);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            return _frameSync.ApplyGatewayPush(opCode, payload);
        }
    }

    public readonly struct ShooterClientGatewayInputSubmitResult
    {
        public readonly ShooterClientInputSubmitResult Local;
        public readonly ShooterGatewayBattleInputResult Remote;

        public ShooterClientGatewayInputSubmitResult(in ShooterClientInputSubmitResult local, in ShooterGatewayBattleInputResult remote)
        {
            Local = local;
            Remote = remote;
        }
    }

    public readonly struct ShooterClientInputSubmitResult
    {
        public readonly int AcceptedInputs;
        public readonly int RequestedFrame;
        public readonly ShooterInputPacket Packet;

        public ShooterClientInputSubmitResult(int acceptedInputs, int requestedFrame, in ShooterInputPacket packet)
        {
            AcceptedInputs = acceptedInputs;
            RequestedFrame = requestedFrame;
            Packet = packet;
        }

        public ShooterClientInputSubmitResult WithRequestedFrame(int requestedFrame)
        {
            return new ShooterClientInputSubmitResult(AcceptedInputs, requestedFrame, in Packet);
        }
    }
}
