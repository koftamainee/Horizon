using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Horizon.Physics;

public sealed class PhysicsSystem
{
    private readonly List<PhysicsBody> _bodies = new();
    private List<Rectangle> _staticColliders = new();

    public void Register(PhysicsBody body)
    {
        _bodies.Add(body);
    }

    public void Unregister(PhysicsBody body)
    {
        _bodies.Remove(body);
    }

    public void SetStaticColliders(List<Rectangle> colliders)
    {
        _staticColliders = colliders;
    }

    public void Step(float dt)
    {
        foreach (var body in _bodies)
            body.Update(dt, _staticColliders);
    }
}