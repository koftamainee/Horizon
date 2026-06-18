    using Horizon.States;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    namespace Horizon.Core;

    public sealed class GameStateManager
    {
        public IGameState CurrentGameState { get; private set; }

        public void ChangeState(IGameState newState)
        {
            CurrentGameState?.OnExit();
            CurrentGameState = newState;
            CurrentGameState?.OnEnter();
        }

        public void Update(GameTime gameTime)
        {
            CurrentGameState?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            CurrentGameState?.Draw(spriteBatch);
        }
    }