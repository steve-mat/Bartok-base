using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum PlayerType {
    HUMAN,
    AI
}

[System.Serializable]
public class Player {

	public PlayerType type = PlayerType.AI;
    public int playerNum;

    public List<CardBartok> hand;

    public SlotDef handSlotDef;

    public CardBartok AddCard(CardBartok eCB) {
        if(hand == null) {
            hand = new List<CardBartok>();
        }

        hand.Add(eCB);

        if(type == PlayerType.HUMAN) {
            CardBartok[] cards = hand.ToArray();
            cards = cards.OrderBy(cd => cd.rank).ToArray();

            hand = new List<CardBartok>(cards);
        }

        eCB.SetSortingLayerName("10");
        eCB.eventualSortLayer = handSlotDef.layerName;



        FanHand();
        return eCB;
    }

    public CardBartok RemoveCard(CardBartok cb) {
        hand.Remove(cb);
        FanHand();
        return cb;
    }

    private void FanHand() {

        float startRot = 0;
        startRot = handSlotDef.rot;

        if(hand.Count > 1) {
            startRot = startRot + Bartok.S.handFanDegrees * (hand.Count - 1) / 2;
        }

        Vector3 pos;
        float rot;
        Quaternion rotQ;

        for(int i = 0; i < hand.Count; i++) {
            rot = startRot - Bartok.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);

            pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
            pos = rotQ * pos;
            pos = pos + handSlotDef.pos;
            pos.z = -0.5f * i;

            if(Bartok.S.phase != TurnPhase.IDLE) {
                hand[i].timeStart = 0;
            }

            hand[i].MoveTo(pos, rotQ);
            hand[i].state = CBState.TOHAND;

            //hand[i].transform.localPosition = pos;
            //hand[i].transform.rotation = rotQ;
            //hand[i].state = CBState.HAND;

            hand[i].faceUp = (type == PlayerType.HUMAN);
            hand[i].eventualSortOrder = i * 4;
        }

    }

    internal void TakeTurn() {

        Utils.tr(Utils.RoundToPlaces(Time.time), "Player.TakeTurn");

        if(type == PlayerType.HUMAN) {
            return;
        }

        Bartok.S.phase = TurnPhase.WAITING;

        CardBartok cb;

        List<CardBartok> validCards = new List<CardBartok>();
        foreach(CardBartok tCB in hand) {
            if(Bartok.S.ValidPlay(tCB)) {
                validCards.Add(tCB);
            }
        }

        if(validCards.Count == 0) {
            cb = AddCard(Bartok.S.Draw());
            cb.callbackPlayer = this;
            return;
        }

        cb = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cb);
        Bartok.S.MoveToTarget(cb);
        cb.callbackPlayer = this;

    }

    public void CBCallback(CardBartok tCB) {
        Utils.tr(Utils.RoundToPlaces(Time.time), "Player.CBCallback()", tCB.name, "Player " + playerNum);
        Bartok.S.PassTurn();
    }
}
