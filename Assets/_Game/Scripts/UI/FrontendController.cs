using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Boot → notice → title → main menu → save select / credits → loading → game.
/// Screens are siblings in Frontend.uxml; only one is visible at a time.</summary>
[RequireComponent(typeof(UIDocument))]
public class FrontendController : MonoBehaviour
{
    public FontSet fonts;

    static readonly string[] ScreenNames = { "scr-boot", "scr-notice", "scr-title", "scr-menu", "scr-saves", "scr-credits", "scr-loading" };
    VisualElement root, toast;
    MenuList active;
    Action onAny, onBack;
    string savesMode;
    int pendingIndex = -1;
    float pendingTimer;
    bool loading;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        toast = root.Q("toast");
        fonts.Apply(root);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 1;
        if (GameSession.ReturnToMenu)
        {
            GameSession.ReturnToMenu = false;
            ShowMenu();
        }
        else StartCoroutine(Boot());
    }

    // ---------- screen switching ----------
    void Show(string name)
    {
        foreach (var s in ScreenNames)
        {
            var el = root.Q(s);
            if (s != name) { el.AddToClassList("hidden"); continue; }
            el.RemoveFromClassList("hidden");
            el.AddToClassList("fade-out");
            el.schedule.Execute(() => el.RemoveFromClassList("fade-out")).StartingIn(30);
        }
        active = null;
        onAny = null;
        onBack = null;
        pendingIndex = -1;
    }

    void Toast(string text) => UIKit.Notice(toast, text, false, 2600);

    IEnumerator Boot()
    {
        Show("scr-boot");
        yield return new WaitForSecondsRealtime(2.6f);
        Show("scr-notice");
        onAny = ShowTitle;
    }

    void ShowTitle()
    {
        Show("scr-title");
        var press = root.Q("press-any");
        press.schedule.Execute(() => press.style.opacity = press.resolvedStyle.opacity > 0.5f ? 0.35f : 1f).Every(900);
        onAny = ShowMenu;
    }

    // ---------- main menu ----------
    void ShowMenu()
    {
        Show("scr-menu");
        var list = root.Q("menu-list");
        list.Clear();
        var menu = new MenuList(list, alignRight: true);
        var recent = SaveSystem.MostRecent();
        menu.Add("Continue", () => StartGame(recent), recent != null);
        if (recent != null) list.Add(UIKit.ContinueCard(recent));
        menu.Add("New game", () => ShowSaves("new"));
        menu.Add("Load game", () => ShowSaves("load"));
        menu.Add("Tellings", () => Toast("Finish a chapter to see its tellings."));
        menu.Add("Options", () => Toast("Options arrive in the next build."));
        menu.Add("Credits", ShowCredits);
        menu.Add("Quit", Quit);
        fonts.Apply(root);
        active = menu;
        onBack = ShowTitle;
    }

    void ShowCredits()
    {
        Show("scr-credits");
        onBack = ShowMenu;
        onAny = null;
    }

    // ---------- save select ----------
    void ShowSaves(string mode)
    {
        Show("scr-saves");
        savesMode = mode;
        root.Q<Label>("saves-eyebrow").text = mode == "new" ? "NEW GAME · CHOOSE A SLOT" : "LOAD GAME";
        var host = root.Q("slot-list");
        host.Clear();
        var list = new MenuList(host, horizontal: true);
        foreach (var slot in SaveSystem.Slots)
        {
            if (mode == "new" && slot == "auto") continue;
            var data = SaveSystem.Load(slot);
            int index = list.Count;
            var s = slot;
            list.AddCustom(UIKit.SlotCard(slot, data), () => OnSlot(s, index));
        }
        fonts.Apply(root);
        active = list;
        onBack = ShowMenu;
    }

    void OnSlot(string slot, int index)
    {
        var data = SaveSystem.Load(slot);
        if (savesMode == "load")
        {
            if (data == null) Toast("Nothing told here yet.");
            else StartGame(data);
            return;
        }
        if (data != null && !(pendingIndex == index && pendingTimer > 0))
        {
            pendingIndex = index;
            pendingTimer = 3f;
            Toast("Press again to overwrite this telling.");
            return;
        }
        GameSession.NewGame(slot);
        StartLoad(GameSession.Current.scene);
    }

    void DeleteSelected()
    {
        if (savesMode != "load" || active == null) return;
        string slot = SaveSystem.Slots[active.Index];
        if (SaveSystem.Load(slot) == null) return;
        if (!(pendingIndex == active.Index && pendingTimer > 0))
        {
            pendingIndex = active.Index;
            pendingTimer = 3f;
            Toast("Press delete again to erase this telling.");
            return;
        }
        SaveSystem.Delete(slot);
        Toast("Telling erased.");
        ShowSaves("load");
    }

    // ---------- loading ----------
    void StartGame(SaveData d)
    {
        if (d == null) return;
        GameSession.Current = d;
        StartLoad(d.scene);
    }

    void StartLoad(string scene)
    {
        if (loading) return;
        loading = true;
        Show("scr-loading");
        StartCoroutine(LoadRoutine(scene));
    }

    IEnumerator LoadRoutine(string scene)
    {
        var spinner = root.Q("spinner");
        float t = 0;
        var op = SceneManager.LoadSceneAsync(scene);
        op.allowSceneActivation = false;
        while (t < 2.4f || op.progress < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            spinner.style.rotate = new Rotate(t * 120f);
            yield return null;
        }
        op.allowSceneActivation = true;
    }

    /// <summary>Used by AutoShot to photograph each screen without input.</summary>
    public void ShowForCapture(string which)
    {
        StopAllCoroutines();
        switch (which)
        {
            case "boot": Show("scr-boot"); break;
            case "notice": Show("scr-notice"); break;
            case "title": ShowTitle(); break;
            case "menu": ShowMenu(); break;
            case "saves": ShowSaves("load"); break;
            case "credits": ShowCredits(); break;
            case "loading": Show("scr-loading"); break;
        }
        onAny = null;
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        pendingTimer -= Time.unscaledDeltaTime;
        if (loading) return;
        if (onAny != null && InputUtil.Any)
        {
            var a = onAny;
            onAny = null;
            a();
            return;
        }
        if (onBack != null && InputUtil.Back)
        {
            onBack();
            return;
        }
        if (active == null) return;
        if (InputUtil.Delete) { DeleteSelected(); return; }
        active.Update();
    }
}
