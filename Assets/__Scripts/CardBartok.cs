using UnityEngine;
using System.Collections.Generic;

public enum CBState {
    DRAWPILE,
    TOHAND,
    HAND,
    TOTARGET,
    TARGET,
    DISCARD,
    TO,
    IDLE
};

public class CardBartok : Card {


    public static float MOVE_DURATION = 0.5f;
    public static string MOVE_EASING = Easing.InOut;
    public static float CARD_HEIGHT = 3.5f;
    public static float CARD_WIDTH = 2.0f;

    public CBState state = CBState.DRAWPILE;

    public List<Vector3> bezierPts;
    public List<Quaternion> bezierRots;
    public float timeStart;
    public float timeDuration;

    public int eventualSortOrder;
    public string eventualSortLayer;

    public GameObject reportFinishTo = null;
    public Player callbackPlayer = null;


    void Awake() {
        callbackPlayer = null;
    }

    void Update() {

        switch(state) {
            case CBState.TOHAND:
            case CBState.TOTARGET:
            case CBState.TO:
                float u = (Time.time - timeStart) / timeDuration;

                float uC = Easing.Ease(u, MOVE_EASING);

                if(u < 0) {
                    transform.localPosition = bezierPts[0];
                    transform.rotation = bezierRots[0];
                    return;
                } else if(u >= 1) {
                    uC = 1;
                    if(state == CBState.TOHAND) {
                        state = CBState.HAND;
                    }
                    if(state == CBState.TOTARGET) {
                        state = CBState.TOTARGET; //target?
                    }
                    if(state == CBState.TO) {
                        state = CBState.IDLE;
                    }

                    transform.localPosition = bezierPts[bezierPts.Count - 1];
                    transform.rotation = bezierRots[bezierPts.Count - 1];

                    timeStart = 0;

                    if(reportFinishTo != null) {
                        reportFinishTo.SendMessage("CBCallback", this);
                        reportFinishTo = null;
                    } else if(callbackPlayer != null) {
                        callbackPlayer.CBCallback(this);
                        callbackPlayer = null;
                    } else {

                    }
                } else {
                    Vector3 pos = Utils.Bezier(uC, bezierPts);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, bezierRots);
                    transform.rotation = rotQ;

                    if(u > 0.5f && spriteRenderers[0].sortingOrder != eventualSortOrder) {
                        SetSortOrder(eventualSortOrder);
                    }
                    if(u > 0.75f && spriteRenderers[0].sortingLayerName != eventualSortLayer) {
                        SetSortingLayerName(eventualSortLayer);
                    }
                }

                break;
        }

    }

    public void MoveTo(Vector3 ePos, Quaternion eRot) {

        bezierPts = new List<Vector3>();
        bezierPts.Add(transform.localPosition);
        bezierPts.Add(ePos);

        bezierRots = new List<Quaternion>();
        bezierRots.Add(transform.rotation);
        bezierRots.Add(eRot);

        if(timeStart == 0) {
            timeStart = Time.time;
        }

        timeDuration = MOVE_DURATION;

        state = CBState.TO;

    }

    public void MoveTo(Vector3 ePos) {
        MoveTo(ePos, Quaternion.identity);
    }

    public override void OnMouseUpAsButton() {
        Bartok.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }

}
