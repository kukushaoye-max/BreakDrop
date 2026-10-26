using System.Runtime.InteropServices;
using UnityEngine;

namespace BreakDrop
{
    // Read host coordinates in logical pixels, then map them to Unity render pixels.
    [DefaultExecutionOrder(-1100)]
    public sealed class MobileViewport : MonoBehaviour
    {
        private readonly float[] metrics = new float[8];
        private float nextPoll;
        private int requestedWidth, requestedHeight;
        private static bool hasHostArea;
        private static Rect hostArea;
        private static Vector2 hostSize;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int BD_ReadViewport([Out] float[] values);
#endif
        public static Rect SafeArea
        {
            get
            {
                if (!hasHostArea) return Screen.safeArea;
                float sx = Screen.width / hostSize.x, sy = Screen.height / hostSize.y;
                return new Rect(hostArea.x * sx, hostArea.y * sy, hostArea.width * sx, hostArea.height * sy);
            }
        }

        private void Awake() { Poll(); }
        private void Update()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + .2f;
            Poll();
        }
        private void OnApplicationPause(bool paused) { if (!paused) nextPoll = 0; }
        private void OnDestroy() { hasHostArea = false; }
        private void Poll()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (BD_ReadViewport(metrics) == 0 || metrics[0] <= 0 || metrics[1] <= 0) return;
            hostSize = new Vector2(metrics[0], metrics[1]);
            hostArea = ResolveSafeArea(metrics[0], metrics[1], metrics[2], metrics[3],
                metrics[4], metrics[5], metrics[6]);
            hasHostArea = true;
            int width = Mathf.RoundToInt(metrics[0] * metrics[7]);
            int height = Mathf.RoundToInt(metrics[1] * metrics[7]);
            if (width != requestedWidth || height != requestedHeight)
            {
                requestedWidth = width; requestedHeight = height;
                Screen.SetResolution(width, height, false);
                Debug.Log("[BreakDrop Viewport] canvas=" + width + "x" + height + " safe=" + hostArea);
            }
#endif
        }

        // WeChat reports top-left coordinates; Unity uses bottom-left coordinates.
        public static Rect ResolveSafeArea(float width, float height, float left, float top,
            float right, float bottom, float capsuleBottom)
        {
            left = Mathf.Clamp(left, 0, width); right = Mathf.Clamp(right, 0, width);
            top = Mathf.Clamp(top, 0, height); bottom = Mathf.Clamp(bottom, 0, height);
            if (right <= left || bottom <= top) { left = top = 0; right = width; bottom = height; }
            if (capsuleBottom > 0 && capsuleBottom + 8 < bottom)
                top = Mathf.Max(top, capsuleBottom + 8);
            return new Rect(left, height - bottom, right - left, bottom - top);
        }
    }
}

