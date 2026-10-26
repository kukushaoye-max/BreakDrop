using UnityEngine;

namespace BreakDrop
{
    [DefaultExecutionOrder(-1000)]
    public sealed class BreakDropBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<MobileViewport>() == null) gameObject.AddComponent<MobileViewport>();
            Application.targetFrameRate = 60;
            Physics2D.gravity = new Vector2(0f, -14f);
            Time.fixedDeltaTime = 1f / 60f;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Physics2D.velocityIterations = 12;
            Physics2D.positionIterations = 8;

            BreakDropGameManager manager = GetComponent<BreakDropGameManager>();
            if (manager == null)
            {
                manager = gameObject.AddComponent<BreakDropGameManager>();
            }

            if (GetComponent<BreakDropHud>() == null)
            {
                gameObject.AddComponent<BreakDropHud>();
            }

            manager.ShowHome();
#if UNITY_EDITOR
            if (SupportSampleSession.Active) manager.LoadLevel(SupportSampleSession.Selected);
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] arguments = System.Environment.GetCommandLineArgs();
            int captureIndex = System.Array.IndexOf(arguments, "--breakdrop-capture");
            if (captureIndex >= 0 && captureIndex + 1 < arguments.Length)
                gameObject.AddComponent<TowerVisualCapture>().DirectoryPath = arguments[captureIndex + 1];
#endif
        }
    }
}

