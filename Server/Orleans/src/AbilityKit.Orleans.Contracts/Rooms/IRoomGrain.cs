using Orleans;

namespace AbilityKit.Orleans.Contracts.Rooms;

public interface IRoomGrain : IGrainWithStringKey
{
    Task InitializeAsync(RoomSummary summary, string directoryKey);

    Task<RoomSnapshot> GetSnapshotAsync();

    Task<RoomRuntimeState> GetRuntimeStateAsync();

    Task<JoinRoomResponse> JoinAsync(string accountId);

    Task<JoinRoomResponse> JoinMemberAsync(JoinRoomMemberRequest request);

    Task<RestoreRoomResponse> RestoreAsync(string accountId);

    Task MarkOfflineAsync(string accountId);

    Task LeaveAsync(string accountId);

    Task SetReadyAsync(RoomReadyRequest request);

    Task SubmitGameplayCommandAsync(RoomGameplayCommandRequest request);

    Task<StartRoomBattleResponse> StartBattleAsync(StartRoomBattleRequest request);

    Task CloseAsync(string accountId);
}
