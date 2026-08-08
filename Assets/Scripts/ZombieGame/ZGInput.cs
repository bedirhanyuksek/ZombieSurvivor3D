using UnityEngine;
using UnityEngine.InputSystem;

public static class ZGInput
{
    public static Vector2 Move
    {
        get
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || ZGSettings.InputLockedByMenu)
            {
                return Vector2.zero;
            }

            var x = 0f;
            var y = 0f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed) y += 1f;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }

    public static Vector2 LookDelta => Mouse.current != null && !ZGSettings.InputLockedByMenu ? Mouse.current.delta.ReadValue() : Vector2.zero;
    public static bool FireHeld => Mouse.current != null && !ZGSettings.InputLockedByMenu && Mouse.current.leftButton.isPressed;
    public static bool ReloadPressed => Keyboard.current != null && !ZGSettings.InputLockedByMenu && Keyboard.current.rKey.wasPressedThisFrame;
    public static bool ToggleCameraPressed => Keyboard.current != null && !ZGSettings.InputLockedByMenu && Keyboard.current.vKey.wasPressedThisFrame;
    public static bool JumpPressed => Keyboard.current != null && !ZGSettings.InputLockedByMenu && Keyboard.current.spaceKey.wasPressedThisFrame;
    public static bool RunHeld => Keyboard.current != null && !ZGSettings.InputLockedByMenu && Keyboard.current.leftShiftKey.isPressed;
    public static bool EscapePressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
}
