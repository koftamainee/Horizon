using Horizon.Core;
using Horizon.Entities;
using Horizon.Rendering;
using Horizon.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.States;

public sealed class GameplayState(Game1 game) : IGameState
{
    private Player _player;
    private Camera _camera;
    private Room _room;

    public void OnEnter()
    {
        _room = new Room();
        _player = new Player(game.GraphicsDevice, game.InputSystem, _room);
        _camera = new Camera(game.GraphicsDevice.Viewport);
    }

    public void Update(GameTime gameTime)
    {
        _player.Update(gameTime);
        _camera.Follow(_player.Position);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(transformMatrix: _camera.Transform);
        
        foreach (var collider in _room.Colliders)
            spriteBatch.Draw(CreateTexture(game.GraphicsDevice), collider, Color.Gray);
        
        _player.Draw(spriteBatch);
        spriteBatch.End();
    }

    public void OnExit() {}

    private static Texture2D CreateTexture(GraphicsDevice gd)
    {
        var tex = new Texture2D(gd, 1, 1);
        tex.SetData(new[] { Color.White });
        return tex;
    }
}