using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>In-game UI: HUD (cores, light, objective, notices, prompt, subtitles), choice screen, pause, end of scene.</summary>
[RequireComponent(typeof(UIDocument))]
public class GameHUD : MonoBehaviour
{
    public FontSet fonts;
    public static GameHUD I { get; private set; }
    /// <summary>True while a menu or choice has focus: player and camera ignore input.</summary>
    public static bool InputBlocked { get; private set; }

    VisualElement root, choice, pause, end, notices, interact, coresHost, lightFill;
    Label objQuest, objText, objSub, subtitle, interactText, choiceSpeaker, choiceLine;
    MenuList active, pauseList;
    bool paused;
    readonly Queue<(string speaker, string text, float secs)> lines = new();
    float lineTimer;

    void OnEnable()
    {
        I = this;
        root = GetComponent<UIDocument>().rootVisualElement;
        choice = root.Q("choice");
        pause = root.Q("pause");
        end = root.Q("end");
        notices = root.Q("notices");
        interact = root.Q("interact");
        coresHost = root.Q("cores");
        lightFill = root.Q("light-fill");
        objQuest = root.Q<Label>("obj-quest");
        objText = root.Q<Label>("obj-text");
        objSub = root.Q<Label>("obj-sub");
        subtitle = root.Q<Label>("subtitle");
        interactText = root.Q<Label>("interact-text");
        choiceSpeaker = root.Q<Label>("choice-speaker");
        choiceLine = root.Q<Label>("choice-line");
        subtitle.enableRichText = true;

        pauseList = new MenuList(root.Q("pause-list"));
        pauseList.Add("Resume", () => SetPause(false));
        pauseList.Add("Quit to menu", ReturnToMenu);
        BuildCompass();
        fonts.Apply(root);
        HideInteract();
        subtitle.text = "";
        RefreshCores();
        SetBlocked(false);
    }

    void OnDisable()
    {
        if (I == this) I = null;
        InputBlocked = false;
        Time.timeScale = 1;
    }

    void SetBlocked(bool b)
    {
        InputBlocked = b;
        UnityEngine.Cursor.lockState = b ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = b;
    }

    void Update()
    {
        if (GameSession.Current != null && !paused) GameSession.Current.playSeconds += Time.unscaledDeltaTime;

        if (paused)
        {
            if (InputUtil.Back) { SetPause(false); return; }
            pauseList.Update();
            return;
        }
        if (active != null) { active.Update(); }
        else if (InputUtil.Pause) { SetPause(true); return; }

        lineTimer -= Time.deltaTime;
        if (lineTimer <= 0)
        {
            if (lines.Count > 0)
            {
                var (who, text, secs) = lines.Dequeue();
                subtitle.text = string.IsNullOrEmpty(who) ? text : $"<color=#D4B06A>{who}</color>   {text}";
                lineTimer = secs;
            }
            else subtitle.text = "";
        }
    }

    // ---------- HUD ----------
    public void RefreshCores(float light = 0.5f)
    {
        coresHost.Clear();
        int lit = GameSession.Current?.cores ?? 7;
        coresHost.Add(UIKit.Cores(lit, 0, Mathf.Max(0, 8 - lit)));
        lightFill.style.width = Length.Percent(Mathf.Clamp01(light) * 100);
    }

    public void SetObjective(string quest, string text, string sub = "")
    {
        objQuest.text = quest.ToUpper();
        objText.text = text;
        objSub.text = sub;
    }

    public void Notice(string text, bool ochre = false) => UIKit.Notice(notices, text, ochre);

    public void Say(string speaker, string text, float seconds = 4f) => lines.Enqueue((speaker, text, seconds));

    public void ShowInteract(string text)
    {
        interactText.text = text;
        interact.style.display = DisplayStyle.Flex;
    }

    public void HideInteract() => interact.style.display = DisplayStyle.None;

    // ---------- compass ----------
    const float CompassSpan = 90f; // degrees from centre to each edge of the strip
    VisualElement compass;
    readonly List<(VisualElement el, float bearing)> cardinals = new();
    Transform compassFrom, mainTarget, optTarget;
    VisualElement mainEl, optEl;
    Label mainDist;

    void BuildCompass()
    {
        compass = root.Q("compass");
        string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        for (int i = 0; i < 8; i++)
        {
            var item = UIKit.E("cmp-item");
            item.pickingMode = PickingMode.Ignore;
            if (i % 2 == 0)
            {
                var l = UIKit.L(names[i], "cmp-letter");
                if (i == 0) l.AddToClassList("north");
                item.Add(l);
            }
            else item.Add(UIKit.E("cmp-tick"));
            compass.Add(item);
            cardinals.Add((item, i * 45f));
        }
        mainEl = UIKit.E("cmp-item");
        mainEl.Add(UIKit.E("cmp-marker", "main"));
        mainDist = UIKit.L("", "cmp-dist");
        mainEl.Add(mainDist);
        optEl = UIKit.E("cmp-item");
        optEl.Add(UIKit.E("cmp-marker"));
        compass.Add(optEl);
        compass.Add(mainEl);
    }

    /// <summary>Main objective (diamond + distance) and an optional secondary marker. Pass null to hide.</summary>
    public void SetCompass(Transform from, Transform main, Transform optional = null)
    {
        compassFrom = from;
        mainTarget = main;
        optTarget = optional;
    }

    static float Bearing(Vector3 from, Vector3 to) => Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;

    void PlaceOnCompass(VisualElement el, float bearing, float yaw)
    {
        float delta = Mathf.DeltaAngle(yaw, bearing);
        float w = compass.resolvedStyle.width;
        if (float.IsNaN(w) || Mathf.Abs(delta) > CompassSpan) { el.style.display = DisplayStyle.None; return; }
        el.style.display = DisplayStyle.Flex;
        el.style.left = w * 0.5f + delta / CompassSpan * w * 0.5f;
        el.style.opacity = 1f - Mathf.Clamp01((Mathf.Abs(delta) - CompassSpan * 0.6f) / (CompassSpan * 0.4f));
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (compass == null || cam == null) return;
        float yaw = cam.transform.eulerAngles.y;
        foreach (var (el, b) in cardinals) PlaceOnCompass(el, b, yaw);
        if (mainTarget && mainTarget.gameObject.activeInHierarchy && compassFrom)
        {
            PlaceOnCompass(mainEl, Bearing(compassFrom.position, mainTarget.position), yaw);
            var d = mainTarget.position - compassFrom.position;
            d.y = 0;
            mainDist.text = $"{Mathf.RoundToInt(d.magnitude)} M";
        }
        else mainEl.style.display = DisplayStyle.None;
        if (optTarget && compassFrom) PlaceOnCompass(optEl, Bearing(compassFrom.position, optTarget.position), yaw);
        else optEl.style.display = DisplayStyle.None;
    }

    // ---------- choice ----------
    public void ShowChoice(string speaker, string line, params (string text, string tag, Action onPick)[] options)
    {
        HideInteract();
        choiceSpeaker.text = speaker.ToUpper();
        choiceLine.text = line;
        var host = root.Q("choice-list");
        var list = new MenuList(host);
        list.Clear();
        foreach (var (text, tag, onPick) in options)
        {
            var el = UIKit.E("mi", "choice-opt");
            el.style.flexDirection = FlexDirection.Column;
            el.style.alignItems = Align.FlexStart;
            el.Add(UIKit.E("gem"));
            el.Add(UIKit.L(text, "opt-text", "f-serif"));
            el.Add(UIKit.L(tag.ToUpper(), "opt-tag"));
            var pick = onPick;
            list.AddCustom(el, () =>
            {
                choice.AddToClassList("hidden");
                active = null;
                SetBlocked(false);
                pick?.Invoke();
            });
        }
        fonts.Apply(root);
        choice.RemoveFromClassList("hidden");
        active = list;
        SetBlocked(true);
    }

    // ---------- pause ----------
    void SetPause(bool on)
    {
        paused = on;
        pause.EnableInClassList("hidden", !on);
        Time.timeScale = on ? 0 : 1;
        SetBlocked(on || active != null);
    }

    // ---------- end of scene ----------
    public void ShowEnd(string title, IEnumerable<string> choices)
    {
        root.Q<Label>("end-title").text = title;
        var host = root.Q("end-choices");
        host.Clear();
        foreach (var c in choices)
        {
            var l = UIKit.L("◆  " + c, "f-serif", "parch");
            l.style.fontSize = 20;
            l.style.marginBottom = 6;
            host.Add(l);
        }
        var list = new MenuList(root.Q("end-list"));
        list.Add("Return to main menu", ReturnToMenu);
        fonts.Apply(root);
        end.RemoveFromClassList("hidden");
        active = list;
        SetBlocked(true);
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1;
        GameSession.ReturnToMenu = true;
        SceneManager.LoadScene("Frontend");
    }
}
