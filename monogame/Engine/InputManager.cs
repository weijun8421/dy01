using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace DY01.Engine;

public class InputManager
{
    private KeyboardState _current;
    private KeyboardState _previous;
    private MouseState _mouseCurrent;
    private MouseState _mousePrevious;
    internal Dictionary<string, bool> KeyStates => _keyStates;
    private Dictionary<string, bool> _keyStates = new();
    private Dictionary<string, bool> _prev = new();
    private Dictionary<string, bool> _weaponSwitchPrev = new();

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
        _mousePrevious = _mouseCurrent;
        _mouseCurrent = Mouse.GetState();
        _prev = new Dictionary<string, bool>(_keyStates);

        _keyStates["p1_left"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left);
        _keyStates["p1_right"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right);
        _keyStates["p1_jump"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up);
        _keyStates["p1_dash"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
        _keyStates["p1_shoot"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.J);
        _keyStates["p1_melee"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.K);
        _keyStates["p1_reload"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R);
        _keyStates["p1_reload_pressed"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R) && !_previous.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R);

        _keyStates["p2_left"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left);
        _keyStates["p2_right"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right);
        _keyStates["p2_jump"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad0);
        _keyStates["p2_dash"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemPeriod);
        _keyStates["p2_shoot"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad1);
        _keyStates["p2_melee"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad2);
        _keyStates["p2_reload"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad8);
        _keyStates["p2_reload_pressed"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad8) && !_previous.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad8);

        _keyStates["up"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W);
        _keyStates["down"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S);
        _keyStates["left"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A);
        _keyStates["right"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D);
        _keyStates["enter"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) || _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space);
        _keyStates["escape"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
        _keyStates["key_1"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1);
        _keyStates["key_2"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2);
        _keyStates["key_3"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3);
        _keyStates["key_4"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4);
        _keyStates["key_5"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D5);
        _keyStates["key_m"] = _current.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.M);
    }

    public bool Get(string name) => _keyStates.TryGetValue(name, out bool v) ? v : false;
    public bool Pressed(string name) => (_keyStates.TryGetValue(name, out bool v1) ? v1 : false) && !(_prev.TryGetValue(name, out bool v2) ? v2 : false);

    public List<int> GetWeaponSwitch(int playerIdx)
    {
        var result = new List<int>();
        var rawKeys = playerIdx == 0
            ? new[] { Microsoft.Xna.Framework.Input.Keys.D1, Microsoft.Xna.Framework.Input.Keys.D2, Microsoft.Xna.Framework.Input.Keys.D3, Microsoft.Xna.Framework.Input.Keys.D4, Microsoft.Xna.Framework.Input.Keys.D5 }
            : new[] { Microsoft.Xna.Framework.Input.Keys.NumPad3, Microsoft.Xna.Framework.Input.Keys.NumPad4, Microsoft.Xna.Framework.Input.Keys.NumPad5, Microsoft.Xna.Framework.Input.Keys.NumPad6, Microsoft.Xna.Framework.Input.Keys.NumPad7 };

        for (int i = 0; i < rawKeys.Length; i++)
        {
            bool down = _current.IsKeyDown(rawKeys[i]);
            string key = $"wep_{playerIdx}_{i}";
            bool prev = _weaponSwitchPrev.TryGetValue(key, out bool pv) ? pv : false;
            if (down && !prev)
                result.Add(i);
            _weaponSwitchPrev[key] = down;
        }
        return result;
    }

    public int GetMouseWheelSwitch()
    {
        int delta = _mouseCurrent.ScrollWheelValue - _mousePrevious.ScrollWheelValue;
        if (delta > 0) return 1;  // Scroll up
        if (delta < 0) return -1; // Scroll down
        return 0;
    }
}