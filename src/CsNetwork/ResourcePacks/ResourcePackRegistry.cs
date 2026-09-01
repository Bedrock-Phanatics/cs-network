using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CsNetwork.ResourcePacks;

public sealed class ResourcePackRegistry
{
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<Guid, ResourcePack> _packsById = new();
    private readonly ConcurrentDictionary<(Guid, PackVersion), ResourcePack> _packsByIdAndVersion = new();

    public int Count => _packsById.Count;
    public IReadOnlyCollection<ResourcePack> AllPacks => _packsById.Values.ToArray();

    public void Register(ResourcePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        lock (_syncRoot)
        {
            _packsById[pack.Id] = pack;
            _packsByIdAndVersion[(pack.Id, pack.Version)] = pack;
        }
    }

    public bool TryGet(Guid id, [NotNullWhen(true)] out ResourcePack? pack)
    {
        return _packsById.TryGetValue(id, out pack);
    }

    public bool TryGet(Guid id, PackVersion version, [NotNullWhen(true)] out ResourcePack? pack)
    {
        return _packsByIdAndVersion.TryGetValue((id, version), out pack);
    }

    public bool Remove(Guid id)
    {
        lock (_syncRoot)
        {
            if (_packsById.TryRemove(id, out var pack))
            {
                _packsByIdAndVersion.TryRemove((pack.Id, pack.Version), out _);
                return true;
            }

            return false;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _packsById.Clear();
            _packsByIdAndVersion.Clear();
        }
    }
}
