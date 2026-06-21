using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class JoinRoomHandlerTests
{
    [Fact]
    public void JoinRoom_flow_contract_should_preserve_account_and_room_identity()
    {
        var accountId = "account-a";
        var login = new CreateSessionForAccountResponse("session-a", 3600, null);
        var validation = new ValidateSessionResponse(true, accountId, login.ExpireAtUnixMs);
        var request = new JoinRoomRequest(validation.AccountId!, "cn", "server-a", "room-a");

        Assert.True(validation.IsValid);
        Assert.Equal("account-a", request.AccountId);
        Assert.Equal("cn", request.Region);
        Assert.Equal("server-a", request.ServerId);
        Assert.Equal("room-a", request.RoomId);
    }
}
