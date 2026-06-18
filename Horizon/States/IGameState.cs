using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Horizon.States;

public interface IGameState
{
    public void Update(GameTime gameTime);
    public void Draw(SpriteBatch spriteBatch);
    public void OnEnter();
    public void OnExit();
}