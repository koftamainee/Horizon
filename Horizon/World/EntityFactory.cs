using System;
using System.Collections.Generic;
using MonoGame.Extended.Tilemaps;

namespace Horizon.World;

public class EntityFactory
{
    private readonly Dictionary<string, Func<TilemapObject, int>> _factories = new();

    public void Register(string type, Func<TilemapObject, int> factory)
    {
        _factories[type] = factory;
    }

    public int? Create(TilemapObject obj)
    {
        if (_factories.TryGetValue(obj.Class, out var factory))
            return factory(obj);
        if (_factories.TryGetValue(obj.Name, out factory))
            return factory(obj);
        return null;
    }
}
