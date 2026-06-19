using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Rendering;

public sealed class Camera
{

    private Vector2 _position;

    public float Smoothness { get; set; } = 8f;
    
    public Rectangle DeadZone { get; set; }

    public Matrix Transform { get; private set; }

    public Camera(Viewport viewport)
    {
        int width = viewport.Width / 4;
        int height = viewport.Height / 4;

        DeadZone = new Rectangle(
            (viewport.Width - width) / 3,
            (viewport.Height - height) / 3,
            width,
            height);

        UpdateMatrix();
    }

    public void Follow(Vector2 target, float deltaTime)
    {
        Vector2 screenPosition = target - _position;

        Vector2 desiredPosition = _position;
        
        if (screenPosition.X < DeadZone.Left)
            desiredPosition.X = target.X - DeadZone.Left;
        
        if (screenPosition.X > DeadZone.Right)
            desiredPosition.X = target.X - DeadZone.Right;
        
        if (screenPosition.Y < DeadZone.Top)
            desiredPosition.Y = target.Y - DeadZone.Top;
        
        if (screenPosition.Y > DeadZone.Bottom)
            desiredPosition.Y = target.Y - DeadZone.Bottom;

        float t = MathHelper.Clamp(Smoothness * deltaTime, 0f, 1f);

        _position = Vector2.Lerp(_position, desiredPosition, t);

        UpdateMatrix();
    }

    private void UpdateMatrix()
    {
        Transform = Matrix.CreateTranslation(
            -_position.X,
            -_position.Y,
            0f);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, Transform);
    }

    public Vector2 Position => _position;
}