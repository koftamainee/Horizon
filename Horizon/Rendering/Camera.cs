using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Rendering;

public sealed class Camera(Viewport viewport)
{
    private Vector2 _position;
    
    public Matrix Transform { get; private set; }
    

    public void Follow(Vector2 target)
    {
        _position = target - new Vector2(
            viewport.Width / 2f,
            viewport.Height / 2f
        );
        
        Transform = Matrix.CreateTranslation(-_position.X, -_position.Y, 0f);
    }
}