using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using WoguaShooter.Core;
using WoguaShooter.Lua;
using WoguaShooter.Update;

namespace WoguaShooter.Diagnostics
{
    public class RuntimeSmokeCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateWhenRequested()
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            bool requested = System.Array.Exists(arguments, item => item == "-smokeCapture");
            if (!requested)
                return;

            GameObject runner = new GameObject("RuntimeSmokeCapture");
            DontDestroyOnLoad(runner);
            runner.AddComponent<RuntimeSmokeCapture>();
            Application.runInBackground = true;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3f);

            GameManager manager = FindFirstObjectByType<GameManager>();
            LuaRuntimeBridge lua = FindFirstObjectByType<LuaRuntimeBridge>();
            Debug.Log($"PLAYER_SMOKE game={(manager != null)}, lua={(lua != null && lua.IsLoaded)}, time={(manager != null ? manager.ElapsedTime : 0f):0.0}");

            string updateAction = GetArgumentValue("-updateAction");
            if (!string.IsNullOrEmpty(updateAction))
                yield return RunUpdateAction(updateAction);

            string path = GetArgumentValue("-screenshotPath");
            if (string.IsNullOrEmpty(path))
                path = Path.Combine(Application.persistentDataPath, "game-smoke.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            CaptureCamera(path);

            yield return new WaitForSecondsRealtime(2f);
            Application.Quit(manager != null && lua != null && lua.IsLoaded ? 0 : 1);
        }

        private static IEnumerator RunUpdateAction(string action)
        {
            UpdatePanel panel = FindFirstObjectByType<UpdatePanel>(FindObjectsInactive.Include);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (panel != null && !panel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                Debug.LogError("UPDATE_SMOKE_FAIL: update panel was not shown");
                yield break;
            }

            string buttonName = action == "accept" ? "UpdateButton" : "LaterButton";
            Button button = panel.transform.Find("Dialog/" + buttonName)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("UPDATE_SMOKE_FAIL: button was not found");
                yield break;
            }

            button.onClick.Invoke();
            if (action == "accept")
            {
                string patchPath = Path.Combine(Application.persistentDataPath, "HotUpdate", "game_rules.lua.txt");
                deadline = Time.realtimeSinceStartup + 8f;
                while (!File.Exists(patchPath) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Debug.Log($"UPDATE_SMOKE_ACCEPT patch={File.Exists(patchPath)}, version={PlayerPrefs.GetString("content_version", "none")}");
            }
            else
            {
                yield return null;
                Debug.Log($"UPDATE_SMOKE_LATER hidden={!panel.gameObject.activeInHierarchy}");
            }
        }

        private static void CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            RenderTexture renderTexture = new RenderTexture(1386, 640, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(1386, 640, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, 1386f, 640f), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.Destroy(image);
            renderTexture.Release();
            Object.Destroy(renderTexture);
        }

        private static string GetArgumentValue(string name)
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                    return arguments[i + 1];
            }
            return null;
        }
    }
}
