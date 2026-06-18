using Horizon.Core;
using Horizon.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.States;

public sealed class GameplayState(Game1 game) : IGameState
{
    private Player _player;
    
    public void Update(GameTime gameTime)
    {
        _player.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();
        
        _player.Draw(spriteBatch);
        
        spriteBatch.End();
    }

    public void OnEnter()
    {
        _player = new Player(game);
    }

    public void OnExit()
    {
    }
}