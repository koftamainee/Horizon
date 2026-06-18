using System;
using Horizon.Core;
using Horizon.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Horizon.Entities;

public sealed class Player : Entity
{
    private Game1 _game;
    
    private readonly Texture2D _texture;
    private readonly Vector2 _size = new Vector2(32, 32);

    private const float Gravity = 1000f;
    private const float JumpSpeed = -500f;
    private const float MaxHorizontalSpeed = 200f;
    private bool _isOnGround = true;

    public Player(Game1 game)
    {
        _game = game;
        _texture = new Texture2D(_game.GraphicsDevice, (int)_size.X, (int)_size.Y);
        Color[] pixels = new Color[(int)_size.X * (int)_size.Y];
        Array.Fill(pixels, Color.White);
        _texture.SetData(pixels);
        Position = new Vector2(100, 100);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        var inputX = 0f;
        if (_game.InputSystem.IsActionPressed(InputSystem.Actions.MoveLeft)) inputX -= 1;
        if (_game.InputSystem.IsActionPressed(InputSystem.Actions.MoveRight)) inputX += 1;

        float targetVelocityX = inputX * MaxHorizontalSpeed;
        
        var velocity = Velocity;
        velocity.X = targetVelocityX;
        
        velocity.Y += Gravity * dt;
        
        if (_isOnGround && _game.InputSystem.IsActionPressed(InputSystem.Actions.Jump))
        {
            Console.Out.WriteLine("Jump");
            velocity.Y = JumpSpeed;
            _isOnGround = false;
        }

        Velocity = velocity;
        
        var position = Position;
        position += Velocity * dt;
        Position = position;
        
        float groundY = 500;
        if (Position.Y + _size.Y > groundY)
        {
            position = Position;
            position.Y = groundY - _size.Y;
            Position = position;

            velocity = Velocity;
            velocity.Y = 0;
            Velocity = velocity;

            _isOnGround = true;
        }
        
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, 
            new Rectangle((int)Position.X, (int)Position.Y, (int)_size.X, (int)_size.Y), 
            Color.Red);
        base.Draw(spriteBatch);
    }
}