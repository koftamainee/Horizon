using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Tilemaps;

namespace Horizon.World;

public class Room
{
    public string Id { get; init; } = "default";
    public Tilemap Tilemap { get; set; }
    public List<CollisionShape2D> ColliderShapes { get; } = new();
    public List<Vector2[]> PolygonVertices { get; } = new(); // null = rectangle, non-null = polygon/triangle vertices for debug
    public HashSet<int> OneWayIds { get; } = new();
    public HashSet<int> HazardIds { get; } = new();
    public List<BoundingBox2D> SpawnBounds { get; } = new();
    public Dictionary<string, Vector2> NamedSpawns { get; } = new();
    public List<TilemapObject> EntityObjects { get; } = new();
    public List<TriggerZone> Triggers { get; } = new();
    public BoundingBox2D Bounds { get; set; }
    public RoomState State { get; set; } = new();
    public RoomVisitState Visit { get; set; } = new();

    public static Room LoadFromTiled(string contentPath, ContentManager content)
    {
        var tilemap = content.Load<Tilemap>(contentPath);

        var room = new Room
        {
            Id = contentPath,
            Tilemap = tilemap,
        };

        var wb = tilemap.WorldBounds;
        room.Bounds = BoundingBox2D.CreateFromPositionAndSize(new Vector2(wb.X, wb.Y), new Vector2(wb.Width, wb.Height));

        foreach (var layer in tilemap.Layers)
        {
            if (layer is TilemapObjectLayer objLayer)
            {
                switch (layer.Name)
                {
                    case "Collisions":
                        LoadCollisionLayer(room, objLayer);
                        break;
                    case "Hazards":
                        LoadHazardLayer(room, objLayer);
                        break;
                    case "Triggers":
                        LoadTriggersLayer(room, objLayer);
                        break;
                    case "Spawns":
                        LoadSpawnsLayer(room, objLayer);
                        break;
                    case "Entities":
                        LoadEntitiesLayer(room, objLayer);
                        break;
                }
            }
        }

        return room;
    }

    private static void LoadCollisionLayer(Room room, TilemapObjectLayer layer)
    {
        foreach (var obj in layer.Objects)
        {
            if (!obj.IsVisible) continue;

            var isOneWay = obj.Properties.GetBool("one_way", false);

            if (obj is TilemapRectangleObject rectObj)
            {
                room.ColliderShapes.Add(new CollisionShape2D(
                    BoundingBox2D.CreateFromPositionAndSize(obj.Position, rectObj.Size)));
                room.PolygonVertices.Add(null);
                if (isOneWay)
                    room.OneWayIds.Add(room.ColliderShapes.Count - 1);
            }
            else if (obj is TilemapPolygonObject polyObj)
            {
                var worldPoints = polyObj.WorldPoints;
                var convex = IsConvex(worldPoints);

                if (convex)
                {
                    room.ColliderShapes.Add(new CollisionShape2D(polyObj.Shape));
                    room.PolygonVertices.Add(worldPoints);
                    if (isOneWay)
                        room.OneWayIds.Add(room.ColliderShapes.Count - 1);
                }
                else
                {
                    var triangles = Triangulate(worldPoints);
                    foreach (var tri in triangles)
                    {
                        room.ColliderShapes.Add(new CollisionShape2D(new BoundingPolygon2D(tri)));
                        room.PolygonVertices.Add(tri);
                        if (isOneWay)
                            room.OneWayIds.Add(room.ColliderShapes.Count - 1);
                    }
                }
            }
        }
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    private static float ComputeSignedArea(Vector2[] vertices)
    {
        float area = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            int j = (i + 1) % vertices.Length;
            area += Cross(vertices[i], vertices[j]);
        }
        return area * 0.5f;
    }

    private static bool IsConvex(Vector2[] vertices)
    {
        int n = vertices.Length;
        if (n < 3) return false;

        float? sign = null;
        for (int i = 0; i < n; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % n];
            var c = vertices[(i + 2) % n];
            var cross = Cross(b - a, c - b);
            if (Math.Abs(cross) < 1e-6f) continue;
            if (sign == null)
                sign = Math.Sign(cross);
            else if (Math.Sign(cross) != sign)
                return false;
        }
        return true;
    }

    private static List<Vector2[]> Triangulate(Vector2[] polygon)
    {
        var result = new List<Vector2[]>();
        var verts = new List<Vector2>(polygon);
        int n = verts.Count;
        if (n < 3) return result;
        if (n == 3) { result.Add(verts.ToArray()); return result; }

        // Ensure CCW winding (positive signed area) as required by BoundingPolygon2D
        if (ComputeSignedArea(verts.ToArray()) < 0)
            verts.Reverse();

        var indices = new List<int>();
        for (int i = 0; i < verts.Count; i++) indices.Add(i);

        while (indices.Count > 3)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                if (IsEar(verts, indices, i))
                {
                    int prev = (i - 1 + indices.Count) % indices.Count;
                    int next = (i + 1) % indices.Count;

                    result.Add(new[]
                    {
                        verts[indices[prev]],
                        verts[indices[i]],
                        verts[indices[next]],
                    });

                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) break; // degenerate polygon, stop
        }

        if (indices.Count == 3)
        {
            result.Add(new[]
            {
                verts[indices[0]],
                verts[indices[1]],
                verts[indices[2]],
            });
        }

        return result;
    }

    private static bool IsEar(List<Vector2> verts, List<int> indices, int i)
    {
        int n = indices.Count;
        int prev = indices[(i - 1 + n) % n];
        int curr = indices[i];
        int next = indices[(i + 1) % n];

        var a = verts[prev];
        var b = verts[curr];
        var c = verts[next];

        // For CCW: convex means cross > 0
        if (Cross(b - a, c - b) <= 0) return false;

        // No other vertex can be strictly inside the triangle
        for (int j = 0; j < n; j++)
        {
            int k = indices[j];
            if (k == prev || k == curr || k == next) continue;
            if (PointInTriangle(verts[k], a, b, c))
                return false;
        }

        return true;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float denom = dot00 * dot11 - dot01 * dot01;
        if (Math.Abs(denom) < 1e-10f) return false;
        float invDenom = 1 / denom;
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u > 0 && v > 0 && u + v < 1;
    }

    private static void LoadHazardLayer(Room room, TilemapObjectLayer layer)
    {
        foreach (var obj in layer.Objects)
        {
            if (!obj.IsVisible) continue;

            if (obj is TilemapRectangleObject rectObj)
            {
                room.ColliderShapes.Add(new CollisionShape2D(
                    BoundingBox2D.CreateFromPositionAndSize(obj.Position, rectObj.Size)));
                room.PolygonVertices.Add(null);
                room.HazardIds.Add(room.ColliderShapes.Count - 1);
            }
            else if (obj is TilemapPolygonObject polyObj)
            {
                var worldPoints = polyObj.WorldPoints;
                var convex = IsConvex(worldPoints);

                if (convex)
                {
                    room.ColliderShapes.Add(new CollisionShape2D(polyObj.Shape));
                    room.PolygonVertices.Add(worldPoints);
                    room.HazardIds.Add(room.ColliderShapes.Count - 1);
                }
                else
                {
                    var triangles = Triangulate(worldPoints);
                    foreach (var tri in triangles)
                    {
                        room.ColliderShapes.Add(new CollisionShape2D(new BoundingPolygon2D(tri)));
                        room.PolygonVertices.Add(tri);
                        room.HazardIds.Add(room.ColliderShapes.Count - 1);
                    }
                }
            }
        }
    }

    private static void LoadTriggersLayer(Room room, TilemapObjectLayer layer)
    {
        foreach (var obj in layer.Objects)
        {
            if (!obj.IsVisible) continue;
            if (obj is not TilemapRectangleObject rectObj) continue;

            room.Triggers.Add(new TriggerZone
            {
                Bounds = BoundingBox2D.CreateFromPositionAndSize(obj.Position, rectObj.Size),
                Type = obj.Class,
                TargetRoom = obj.Properties.GetString("target_room", ""),
                TargetSpawn = obj.Properties.GetString("target_spawn", ""),
            });
        }
    }

    private static void LoadSpawnsLayer(Room room, TilemapObjectLayer layer)
    {
        foreach (var obj in layer.Objects)
        {
            if (!obj.IsVisible) continue;

            BoundingBox2D bounds;
            if (obj is TilemapRectangleObject rectObj)
            {
                bounds = BoundingBox2D.CreateFromPositionAndSize(
                    obj.Position, rectObj.Size);
            }
            else
            {
                bounds = BoundingBox2D.CreateFromPositionAndSize(
                    obj.Position - new Vector2(24, 24), new Vector2(48, 48));
            }

            room.SpawnBounds.Add(bounds);

            if (!string.IsNullOrEmpty(obj.Name))
                room.NamedSpawns[obj.Name] = bounds.Center;
        }
    }

    private static void LoadEntitiesLayer(Room room, TilemapObjectLayer layer)
    {
        foreach (var obj in layer.Objects)
        {
            if (!obj.IsVisible) continue;
            room.EntityObjects.Add(obj);
        }
    }
}
