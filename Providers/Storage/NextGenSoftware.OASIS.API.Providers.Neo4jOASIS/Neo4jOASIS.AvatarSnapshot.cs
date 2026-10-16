using System;
using System.IO;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static Avatar StageAvatar(IAvatar source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var staged = OasisJson.Deserialize<Avatar>(OasisJson.Serialize(source));
        if (staged == null) throw new InvalidDataException("Neo4j avatar snapshot is null.");
        if (staged.Id == Guid.Empty) staged.Id = Guid.NewGuid();
        staged.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = staged.Id.ToString();
        staged.IsNewHolon = false;
        return staged;
    }

    internal static Avatar ReadAvatarSnapshot(string json, string indexedId)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Neo4j avatar canonical snapshot is missing.");
        var avatar = OasisJson.Deserialize<Avatar>(json);
        if (avatar == null || !Guid.TryParse(indexedId, out var id) || id == Guid.Empty || avatar.Id != id)
            throw new InvalidDataException("Neo4j avatar snapshot identity differs from its index.");
        return avatar;
    }

    private static Avatar ReadAvatarNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json))
            throw new InvalidDataException("Neo4j avatar canonical snapshot is missing.");
        return ReadAvatarSnapshot(json as string, node["Id"].As<string>());
    }
}
