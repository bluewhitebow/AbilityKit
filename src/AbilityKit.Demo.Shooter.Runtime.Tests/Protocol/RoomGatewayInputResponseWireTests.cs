using AbilityKit.Protocol.Room;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Protocol;

public sealed class RoomGatewayInputResponseWireTests
{
    [Fact]
    public void SubmitBattleInputResponseRoundTripPreservesResyncDiagnostics()
    {
        var response = new WireSubmitBattleInputRes
        {
            Success = false,
            AcceptedFrame = 42,
            Message = "Input frame is too far ahead.",
            CurrentFrame = 37,
            Status = "RejectedTooFarFuture",
            ShouldResync = true,
            ServerTicks = 987654321L
        };

        var bytes = WireRoomGatewayBinary.Serialize(in response);
        var restored = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputRes>(bytes);

        Assert.False(restored.Success);
        Assert.Equal(42, restored.AcceptedFrame);
        Assert.Equal("Input frame is too far ahead.", restored.Message);
        Assert.Equal(37, restored.CurrentFrame);
        Assert.Equal("RejectedTooFarFuture", restored.Status);
        Assert.True(restored.ShouldResync);
        Assert.Equal(987654321L, restored.ServerTicks);
    }
}
