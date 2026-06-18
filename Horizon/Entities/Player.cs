using System;
using System.Collections.Generic;
using Horizon.Components;
using Horizon.Input;
using Horizon.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.Entities;

public sealed class Player : Entity
{
    private readonly PhysicsBody _body;
    private readonly PlayerController _controller;
    private readonly Texture2D _texture;
    private readonly Vector2 _size = new(32, 32);

    private readonly Room _room;

    public Vector2 Position => _body.Position;

    public Player(GraphicsDevice graphicsDevice, InputSystem input, Room room)
    {
        _room = room;
        
        _body = new PhysicsBody(new Vector2(200, 100), _size);
        _controller = new PlayerController(_body, input);

        _texture = new Texture2D(graphicsDevice, (int)_size.X, (int)_size.Y);
        var pixels = new Color[(int)(_size.X * _size.Y)];
        Array.Fill(pixels, Color.White);
        _texture.SetData(pixels);
    }

    public override void Update(GameTime gameTime)
    {
        _controller.Update(gameTime);
        _body.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _room.Colliders);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture,
            new Rectangle((int)_body.Position.X, (int)_body.Position.Y,
                (int)_size.X, (int)_size.Y),
            Color.Red);
    }
}