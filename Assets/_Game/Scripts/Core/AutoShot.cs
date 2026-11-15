using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Screenshot tour for checking the UI in a build: run the game with  -autoshot "C:\folder".
/// Uses a temporary save folder with sample saves, photographs every screen, then quits. Does nothing otherwise.</summary>
public class AutoShot : MonoBehaviour
{
    static string outDir;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-autoshot");
        if (i < 0 || i + 1 >= args.Length) return;
        outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        SaveSystem.DirOverride = Path.Combine(outDir, "saves");
        var go = new GameObject("AutoShot");
        DontDestroyOnLoad(go);
        go.AddComponent<AutoShot>();
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(0.8f);
        ScreenCapture.CaptureScreenshot(Path.Combine(outDir, name + ".png"));
        yield return new WaitForSecondsRealtime(0.3f);
    }

    IEnumerator Start()
    {
        SampleSaves();
        yield return new WaitForSecondsRealtime(1f);
        var fc = FindAnyObjectByType<FrontendController>();
        foreach (var s in new[] { "boot", "notice", "title", "menu", "saves", "credits", "loading" })
        {
            fc.ShowForCapture(s);
            yield return Shot("frontend-" + s);
        }
        GameSession.Current = SaveSystem.Load("1");
        SceneManager.LoadScene("Marsh");
        yield return new WaitForSecondsRealtime(3f);
        var hud = GameHUD.I;
        hud.SetObjective("The One Who Fell", "Follow the boy through the reeds", "Stay close to his torch");
        hud.Notice("The boy will remember this.", true);
        hud.ShowInteract("Talk to the boy");
        hud.Say("BOY", "Keep low. They hunt anything warm.", 6f);
        yield return Shot("game-hud");
        hud.HideInteract();
        hud.ShowChoice("The boy", "\"You can barely stand. There is a path through the reeds only my family knows. Will you follow me?\"",
            ("Take his hand", "Ask for help", null), ("Stand on your own", "Go alone", null));
        yield return Shot("game-choice");
        hud.ShowEnd("The One Who Fell", new[] { "Stood alone in the hills", "Overheard the bandit camp" });
        yield return Shot("game-end");
        Application.Quit();
    }

    static void SampleSaves()
    {
        var a = new SaveData { slot = "1", playSeconds = 3720, choices = { "Took the boy's hand" }, flags = { "askedForHelp" } };
        SaveSystem.Save(a);
        var b = new SaveData { slot = "2", place = "Gasani's Homestead", chapter = "Chapter I · Three Days", image = "village", cores = 5, playSeconds = 8040,
            choices = { "Stood alone in the hills", "Overheard the bandit camp" } };
        SaveSystem.Save(b, alsoAutosave: false);
    }
}
