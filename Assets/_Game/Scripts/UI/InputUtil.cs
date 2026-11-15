using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Menu-style input read straight from devices (keyboard, mouse, gamepad).</summary>
public static class InputUtil
{
    static Keyboard K => Keyboard.current;
    static Gamepad G => Gamepad.current;
    static Mouse M => Mouse.current;

    static bool KeyDown(params Key[] keys)
    {
        if (K == null) return false;
        foreach (var k in keys)
            if (K[k].wasPressedThisFrame) return true;
        return false;
    }

    public static bool Up => KeyDown(Key.UpArrow, Key.W) || (G != null && (G.dpad.up.wasPressedThisFrame || G.leftStick.up.wasPressedThisFrame));
    public static bool Down => KeyDown(Key.DownArrow, Key.S) || (G != null && (G.dpad.down.wasPressedThisFrame || G.leftStick.down.wasPressedThisFrame));
    public static bool Left => KeyDown(Key.LeftArrow, Key.A) || (G != null && (G.dpad.left.wasPressedThisFrame || G.leftStick.left.wasPressedThisFrame));
    public static bool Right => KeyDown(Key.RightArrow, Key.D) || (G != null && (G.dpad.right.wasPressedThisFrame || G.leftStick.right.wasPressedThisFrame));
    public static bool Submit => KeyDown(Key.Enter, Key.NumpadEnter, Key.Space, Key.E) || (G != null && G.buttonSouth.wasPressedThisFrame);
    public static bool Back => KeyDown(Key.Escape, Key.Backspace) || (G != null && G.buttonEast.wasPressedThisFrame);
    public static bool Pause => KeyDown(Key.Escape) || (G != null && G.startButton.wasPressedThisFrame);
    public static bool Delete => KeyDown(Key.Delete) || (G != null && G.buttonWest.wasPressedThisFrame);
    public static bool Interact => KeyDown(Key.E) || (G != null && G.buttonSouth.wasPressedThisFrame);

    public static bool Any =>
        (K != null && K.anyKey.wasPressedThisFrame) ||
        (M != null && M.leftButton.wasPressedThisFrame) ||
        (G != null && (G.buttonSouth.wasPressedThisFrame || G.startButton.wasPressedThisFrame || G.buttonEast.wasPressedThisFrame));

    public static Vector2 Move
    {
        get
        {
            var v = Vector2.zero;
            if (K != null)
            {
                if (K.wKey.isPressed) v.y += 1;
                if (K.sKey.isPressed) v.y -= 1;
                if (K.dKey.isPressed) v.x += 1;
                if (K.aKey.isPressed) v.x -= 1;
            }
            if (G != null) v += G.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
        }
    }

    public static bool Sprint => (K != null && K.leftShiftKey.isPressed) || (G != null && G.leftStickButton.isPressed);

    public static Vector2 Look(float mouseSens, float padSens)
    {
        var v = Vector2.zero;
        if (M != null) v += M.delta.ReadValue() * mouseSens;
        if (G != null) v += G.rightStick.ReadValue() * padSens * Time.unscaledDeltaTime;
        return v;
    }
}
