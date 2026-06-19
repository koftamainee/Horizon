using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.ECS.Components;

public class SpriteRenderer
{
    public Texture2D Texture;
    public Rectangle Source;
    public Color Tint = Color.White;
}
