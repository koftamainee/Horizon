using Horizon.States;
using Horizon.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Horizon.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    public GameStateManager GameStateManager;
    public InputSystem InputSystem;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GameStateManager = new GameStateManager();
        InputSystem = new InputSystem();
        
        GameStateManager.ChangeState(new GameplayState(this));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        InputSystem.Update(gameTime);
        GameStateManager.Update(gameTime);
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        GameStateManager.Draw(_spriteBatch);

        base.Draw(gameTime);
    }
}