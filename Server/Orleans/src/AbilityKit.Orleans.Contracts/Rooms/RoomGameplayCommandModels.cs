using System.Collections.Generic;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Rooms;

public static class RoomGameplayCommandNames
{
    public const string ConfigureMobaLoadout = "moba.configure_loadout";
}

[GenerateSerializer]
public sealed record RoomGameplayCommandRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string CommandName,
    [property: Id(2)] Dictionary<string, string>? Fields,
    [property: Id(3)] byte[]? Payload)
{
    public static RoomGameplayCommandRequest CreateMobaLoadout(
        string accountId,
        int heroId,
        int teamId,
        int spawnPointId,
        int level,
        int attributeTemplateId,
        int basicAttackSkillId,
        IReadOnlyList<int>? skillIds)
    {
        var fields = new Dictionary<string, string>
        {
            ["heroId"] = heroId.ToString(),
            ["teamId"] = teamId.ToString(),
            ["spawnPointId"] = spawnPointId.ToString(),
            ["level"] = level.ToString(),
            ["attributeTemplateId"] = attributeTemplateId.ToString(),
            ["basicAttackSkillId"] = basicAttackSkillId.ToString()
        };

        if (skillIds is { Count: > 0 })
        {
            fields["skillIds"] = string.Join(",", skillIds);
        }

        return new RoomGameplayCommandRequest(accountId, RoomGameplayCommandNames.ConfigureMobaLoadout, fields, Payload: null);
    }
}
