using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Entities;

public class Entity
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Rectangle Bounds { get; set; }

    public virtual void Update(GameTime gameTime) {}
    public virtual void Draw(SpriteBatch spriteBatch) {}
}