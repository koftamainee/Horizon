using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Horizon.Input;

public class InputSystem
{
    public enum Actions
    {
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Jump,
        Dash,
    }

    private KeyboardState _previousKeyboardState;
    private KeyboardState _currentKeyboardState;

    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    private GamePadState _previousGamePadState;
    private GamePadState _currentGamePadState;

    private readonly Dictionary<Actions, Keys> _keyboardActions = new()
    {
        { Actions.MoveLeft, Keys.A },
        { Actions.MoveRight, Keys.D },
        { Actions.MoveUp, Keys.W },
        { Actions.MoveDown, Keys.S },
        { Actions.Jump, Keys.Space },
        { Actions.Dash, Keys.LeftShift },
    };

    private readonly Dictionary<Actions, Buttons> _gamepadActions = new()
    {
        { Actions.MoveLeft, Buttons.DPadLeft },
        { Actions.MoveRight, Buttons.DPadRight },
        {Actions.MoveUp, Buttons.DPadUp },
        { Actions.MoveDown, Buttons.DPadDown },
        { Actions.Jump, Buttons.X },
        { Actions.Dash, Buttons.RightTrigger },
    };

    public MouseState GetMouseState => _currentMouseState;
    public KeyboardState GetKeyboardState => _currentKeyboardState;
    public GamePadState GetGamePadState => _currentGamePadState;

    public void Update(GameTime gameTime)
    {
        _previousKeyboardState = _currentKeyboardState;
        _currentKeyboardState = Keyboard.GetState();

        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();

        _previousGamePadState = _currentGamePadState;
        _currentGamePadState = GamePad.GetState(PlayerIndex.One);
    }

    public bool IsActionPressed(Actions action)
    {
        var kbButton = _keyboardActions[action];
        var gpButton = _gamepadActions[action];

        var gamepadPressed = _currentGamePadState.IsButtonDown(gpButton);
        var keyboardPressed = _currentKeyboardState.IsKeyDown(kbButton);
        return gamepadPressed || keyboardPressed;
    }

    public bool IsActionJustPressed(Actions action)
    {
        var kbButton = _keyboardActions[action];
        var gpButton = _gamepadActions[action];

        var gamepadPressed = _currentGamePadState.IsButtonDown(gpButton) && _previousGamePadState.IsButtonUp(gpButton);
        var keyboardPressed = _currentKeyboardState.IsKeyDown(kbButton) && _previousKeyboardState.IsKeyUp(kbButton);
        return gamepadPressed || keyboardPressed;
    }

    public bool IsActionReleased(Actions action)
    {
        var kbButton = _keyboardActions[action];
        var gpButton = _gamepadActions[action];

        var gamepadReleased = _currentGamePadState.IsButtonUp(gpButton);
        var keyboardReleased = _currentKeyboardState.IsKeyUp(kbButton);
        return gamepadReleased || keyboardReleased;
    }

    public bool IsActionJustReleased(Actions action)
    {
        var kbButton = _keyboardActions[action];
        var gpButton = _gamepadActions[action];

        var gamepadReleased = _currentGamePadState.IsButtonUp(gpButton) && _previousGamePadState.IsButtonDown(gpButton);
        var keyboardReleased = _currentKeyboardState.IsKeyUp(kbButton) && _previousKeyboardState.IsKeyDown(kbButton);
        return gamepadReleased || keyboardReleased;
    }
}