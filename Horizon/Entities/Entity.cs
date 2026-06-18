using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Entities;

public class Entity
{
    public virtual void Update(GameTime gameTime) {}
    public virtual void Draw(SpriteBatch spriteBatch) {}
}