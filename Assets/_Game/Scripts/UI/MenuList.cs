using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>A focusable list of items (menu entries, save slots, choices) driven by keyboard, gamepad and mouse.
/// Selected item gets the "sel" class; styling lives in Umwami.uss.</summary>
public class MenuList
{
    readonly VisualElement container;
    readonly bool alignRight, horizontal;
    readonly List<VisualElement> items = new();
    readonly List<Action> actions = new();
    readonly List<bool> enabled = new();
    readonly int bornFrame = UnityEngine.Time.frameCount;
    public int Index { get; private set; } = -1;
    public int Count => items.Count;

    public MenuList(VisualElement container, bool alignRight = false, bool horizontal = false)
    {
        this.container = container;
        this.alignRight = alignRight;
        this.horizontal = horizontal;
    }

    /// <summary>Standard text item: gem + label.</summary>
    public VisualElement Add(string text, Action onSubmit, bool isEnabled = true)
    {
        var mi = new VisualElement();
        mi.AddToClassList("mi");
        if (alignRight) mi.AddToClassList("right");
        if (!isEnabled) mi.AddToClassList("disabled");
        var gem = new VisualElement();
        gem.AddToClassList("gem");
        mi.Add(gem);
        var label = new Label(text);
        label.AddToClassList("f-serif");
        mi.Add(label);
        return AddCustom(mi, onSubmit, isEnabled);
    }

    /// <summary>Any element; it is added to the container and becomes selectable.</summary>
    public VisualElement AddCustom(VisualElement el, Action onSubmit, bool isEnabled = true)
    {
        int i = items.Count;
        el.RegisterCallback<PointerEnterEvent>(_ => { if (enabled[i]) Select(i); });
        el.RegisterCallback<ClickEvent>(_ => { if (enabled[i]) { Select(i); Submit(); } });
        if (el.parent == null) container.Add(el);
        items.Add(el);
        actions.Add(onSubmit);
        enabled.Add(isEnabled);
        if (Index < 0 && isEnabled) Select(i);
        return el;
    }

    public void Select(int i)
    {
        if (i < 0 || i >= items.Count) return;
        for (int k = 0; k < items.Count; k++) items[k].EnableInClassList("sel", k == i);
        Index = i;
    }

    void Move(int d)
    {
        int n = items.Count;
        if (n == 0) return;
        int i = Index;
        for (int s = 0; s < n; s++)
        {
            i = (i + d + n) % n;
            if (enabled[i]) break;
        }
        Select(i);
    }

    public void Submit()
    {
        if (Index >= 0 && enabled[Index]) actions[Index]?.Invoke();
    }

    /// <summary>Call once per frame while this list has focus.</summary>
    public void Update()
    {
        if (UnityEngine.Time.frameCount == bornFrame) return; // the key that opened this list must not also pick from it
        if (horizontal ? InputUtil.Right : InputUtil.Down) Move(1);
        else if (horizontal ? InputUtil.Left : InputUtil.Up) Move(-1);
        else if (InputUtil.Submit) Submit();
    }

    public void Clear()
    {
        container.Clear();
        items.Clear();
        actions.Clear();
        enabled.Clear();
        Index = -1;
    }
}
