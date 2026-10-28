using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tactics.Core
{
    // Works with either the new Input System or the legacy Input Manager,
    // whichever the project's "Active Input Handling" setting enables.
    public static class InputShim
    {
#if ENABLE_INPUT_SYSTEM
        static Mouse M => Mouse.current;
        static Keyboard K => Keyboard.current;

        public static Vector2 MousePosition => M != null ? M.position.ReadValue() : Vector2.zero;
        public static bool LeftClickDown => M != null && M.leftButton.wasPressedThisFrame;
        public static bool RightClickDown => M != null && M.rightButton.wasPressedThisFrame;
        public static float Scroll
        {
            get
            {
                float v = M != null ? M.scroll.ReadValue().y : 0f;
                return v == 0f ? 0f : Mathf.Sign(v);
            }
        }
        public static bool RotateLeftDown => K != null && K.qKey.wasPressedThisFrame;
        public static bool RotateRightDown => K != null && K.eKey.wasPressedThisFrame;
        public static bool ConfirmDown => K != null && (K.spaceKey.wasPressedThisFrame || K.enterKey.wasPressedThisFrame);
        public static bool CancelDown => K != null && K.escapeKey.wasPressedThisFrame;

        public static Vector2 Pan
        {
            get
            {
                if (K == null) return Vector2.zero;
                float x = (K.dKey.isPressed || K.rightArrowKey.isPressed ? 1 : 0) - (K.aKey.isPressed || K.leftArrowKey.isPressed ? 1 : 0);
                float y = (K.wKey.isPressed || K.upArrowKey.isPressed ? 1 : 0) - (K.sKey.isPressed || K.downArrowKey.isPressed ? 1 : 0);
                return new Vector2(x, y);
            }
        }
#else
        public static Vector2 MousePosition => Input.mousePosition;
        public static bool LeftClickDown => Input.GetMouseButtonDown(0);
        public static bool RightClickDown => Input.GetMouseButtonDown(1);
        public static float Scroll => Input.mouseScrollDelta.y == 0 ? 0f : Mathf.Sign(Input.mouseScrollDelta.y);
        public static bool RotateLeftDown => Input.GetKeyDown(KeyCode.Q);
        public static bool RotateRightDown => Input.GetKeyDown(KeyCode.E);
        public static bool ConfirmDown => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        public static bool CancelDown => Input.GetKeyDown(KeyCode.Escape);
        public static Vector2 Pan => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }
}
