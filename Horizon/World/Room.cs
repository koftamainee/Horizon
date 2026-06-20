using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tilemaps;
using MonoGame.Extended.Tilemaps.Tiled;

namespace Horizon.World;

public class Room
{
    public string Id { get; init; } = "default";
    public Tilemap Tilemap { get; set; }
    public CollisionShape2D[] CollisionShapes { get; set; } = [];
    public List<BoundingBox2D> Colliders { get; } = new();
    public HashSet<int> OneWayIds { get; } = new();
    public Dictionary<string, Vector2> SpawnPoints { get; } = new();
    public List<TilemapObject> EntityObjects { get; } = new();
    public BoundingBox2D Bounds { get; set; }
    public RoomState State { get; set; } = new();

    public static Room LoadFromTiled(string tmxPath, GraphicsDevice graphicsDevice)
    {
        var dir = Path.GetDirectoryName(tmxPath);
        var parser = new TiledTmxParser(dir, null);
        var tilemap = parser.ParseFromFile(tmxPath, graphicsDevice);

        var room = new Room
        {
            Id = Path.GetFileNameWithoutExtension(tmxPath),
            Tilemap = tilemap,
        };

        var wb = tilemap.WorldBounds;
        room.Bounds = BoundingBox2D.CreateFromPositionAndSize(new Vector2(wb.X, wb.Y), new Vector2(wb.Width, wb.Height));

        foreach (var layer in tilemap.Layers)
        {
            if (layer is TilemapObjectLayer objLayer)
            {
                foreach (var obj in objLayer.Objects)
                {
                    if (!obj.IsVisible) continue;

                    if (obj.Class == "player_spawn")
                    {
                        room.SpawnPoints["player_start"] = obj.Position;
                    }
                    else if (obj is TilemapRectangleObject rectObj)
                    {
                        var isOneWay = obj.Properties.GetBool("one_way", false);
                        room.Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(
                            obj.Position, rectObj.Size));
                        if (isOneWay)
                            room.OneWayIds.Add(room.Colliders.Count - 1);
                    }
                    else if (!string.IsNullOrEmpty(obj.Class))
                    {
                        room.EntityObjects.Add(obj);
                    }
                }
            }
        }

        return room;
    }
}
