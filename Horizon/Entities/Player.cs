using System;
using Horizon.Components;
using Horizon.Input;
using Horizon.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Entities;

public sealed class Player : Entity
{
    private readonly PhysicsBody _body;
    private readonly PlayerController _controller;
    private readonly Texture2D _texture;
    private readonly Vector2 _size = new(32, 32);

    public Vector2 Position => _body.Position;

    public Player(GraphicsDevice graphicsDevice, InputSystem input, PhysicsSystem physics)
    {
        var shape = new BoxShape(_size / 2f, _size / 2f);
        _body = new PhysicsBody(new Vector2(200, 100), shape);
        _controller = new PlayerController(_body, input);

        physics.Register(_body);

        _texture = new Texture2D(graphicsDevice, (int)_size.X, (int)_size.Y);
        var pixels = new Color[(int)(_size.X * _size.Y)];
        Array.Fill(pixels, Color.White);
        _texture.SetData(pixels);
    }

    public override void Update(GameTime gameTime)
    {
        _controller.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture,
            new Rectangle((int)_body.Position.X, (int)_body.Position.Y,
                (int)_size.X, (int)_size.Y),
            Color.Red);
    }
}