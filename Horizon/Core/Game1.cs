using Horizon.Input;
using Horizon.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Screens;

namespace Horizon.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    public SpriteBatch SpriteBatch { get; private set; }
    public InputSystem InputSystem { get; private set; }
    public ScreenManager ScreenManager { get; private set; }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;

        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        InputSystem = new InputSystem();
        ScreenManager = new ScreenManager();
        ScreenManager.ReplaceScreen(new GameplayScreen(this));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        InputSystem.Update(gameTime);
        ScreenManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        ScreenManager.Draw(gameTime);

        base.Draw(gameTime);
    }
}
