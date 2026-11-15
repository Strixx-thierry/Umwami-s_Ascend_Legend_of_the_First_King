using System.Collections;
using UnityEngine;

/// <summary>Act I opening (documents/Act I Story.md beat 4): Rudh wakes by Ikinani, the boy offers a path.
/// Ask for help → follow the boy through the reeds. Go alone → cross the ridge, overhear the uncle's men.</summary>
public class MarshMission : MonoBehaviour
{
    public Transform player, boy;
    public Transform[] boyPath;          // reed route, ends at exitHelp
    public Transform camp, exitHelp, exitAlone;
    public float talkRange = 3f, campRange = 12f, exitRange = 5f, boySpeed = 2.6f, boyWaitDistance = 9f;

    enum State { Start, Talking, FollowBoy, Alone, Done }
    State state;
    int pathIndex;
    bool heardCamp;
    GameHUD hud;

    IEnumerator Start()
    {
        yield return null; // let the HUD initialise
        hud = GameHUD.I;
        if (GameSession.Current == null) GameSession.NewGame("1"); // scene started directly in the editor
        hud.RefreshCores();
        hud.SetObjective("The One Who Fell", "Find a way out of the marsh", "Someone is watching from the reeds");
        hud.SetCompass(player, boy);
        hud.Say("NARRATOR", "I was a boy when I saw him fall. Three lights crossed the sky that night.", 5f);
        hud.Say("NARRATOR", "Only one came down where I could see it.", 4f);
    }

    static float Flat(Transform a, Transform b)
    {
        var d = a.position - b.position;
        d.y = 0;
        return d.magnitude;
    }

    void Update()
    {
        if (hud == null || state == State.Done) return;
        switch (state)
        {
            case State.Start:
                if (Flat(player, boy) < talkRange)
                {
                    hud.ShowInteract("Talk to the boy");
                    if (InputUtil.Interact && !GameHUD.InputBlocked) OfferPath();
                }
                else hud.HideInteract();
                break;
            case State.FollowBoy:
                WalkBoy();
                if (Flat(player, exitHelp) < exitRange && pathIndex >= boyPath.Length) Finish();
                break;
            case State.Alone:
                if (!heardCamp && Flat(player, camp) < campRange) OverhearCamp();
                if (Flat(player, exitAlone) < exitRange) Finish();
                break;
        }
    }

    void OfferPath()
    {
        state = State.Talking;
        hud.ShowChoice("The boy",
            "\"You can barely stand. There is a path through the reeds only my family knows. Will you follow me?\"",
            ("Take his hand", "Ask for help", AskForHelp),
            ("Stand on your own", "Go alone", GoAlone));
    }

    void AskForHelp()
    {
        GameSession.Choose("askedForHelp", "Took the boy's hand");
        hud.Notice("The boy will remember this.", ochre: true);
        hud.SetObjective("The One Who Fell", "Follow the boy through the reeds", "Stay close to his torch");
        hud.Say("BOY", "Keep low. They hunt anything warm.", 4f);
        hud.SetCompass(player, boy);
        state = State.FollowBoy;
    }

    void GoAlone()
    {
        GameSession.Choose("wentAlone", "Stood alone in the hills");
        hud.Say("BOY", "Then go with the sky's luck. The village is past the ridge, where the smoke rises.", 5f);
        hud.SetObjective("The One Who Fell", "Climb the ridge toward the smoke", "Optional · Men gather where no village stands");
        state = State.Alone;
        hud.SetCompass(player, exitAlone, camp);
        StartCoroutine(BoyLeaves());
    }

    IEnumerator BoyLeaves()
    {
        float t = 0;
        while (t < 4f)
        {
            t += Time.deltaTime;
            boy.position += boy.forward * boySpeed * Time.deltaTime;
            yield return null;
        }
        boy.gameObject.SetActive(false);
    }

    void WalkBoy()
    {
        if (pathIndex >= boyPath.Length) { hud.SetCompass(player, exitHelp); return; }
        if (Flat(player, boy) > boyWaitDistance) return; // wait for Rudh
        var target = boyPath[pathIndex].position;
        target.y = boy.position.y;
        var to = target - boy.position;
        if (to.magnitude < 0.3f) { pathIndex++; return; }
        boy.rotation = Quaternion.Slerp(boy.rotation, Quaternion.LookRotation(to), 8f * Time.deltaTime);
        boy.position = Vector3.MoveTowards(boy.position, target, boySpeed * Time.deltaTime);
    }

    void OverhearCamp()
    {
        heardCamp = true;
        GameSession.Choose("knowsThePlot", "Overheard the bandit camp");
        hud.Say("MAN", "The chief rides to Bugesera at first light.", 4f);
        hud.Say("MAN", "Then his brother's word is law. Whatever happens in that house, we blame the sky-man.", 5f);
        hud.Notice("You overheard the uncle's men.", ochre: true);
        hud.SetCompass(player, exitAlone);
    }

    void Finish()
    {
        state = State.Done;
        hud.HideInteract();
        hud.SetCompass(null, null);
        var s = GameSession.Current;
        s.scene = "Marsh";             // next scene (the village) is not built yet
        s.place = "Gasani's Homestead";
        s.chapter = "Chapter I · Three Days";
        s.image = "village";
        SaveSystem.Save(s);
        hud.ShowEnd("The One Who Fell", s.choices);
    }
}
