using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum TurnPhase {
    IDLE,
    PRE,
    WAITING,
    POST,
    GAMEOVER
};

public class Bartok : MonoBehaviour {

    public static Bartok S;

    public static Player CURRENT_PLAYER;

    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;

    public float handFanDegrees = 10f;
    public int numStartingCards = 7;
    public float drawTimeStagger = 0.1f;

    public List<Player> players;
    public CardBartok targetCard;

    public TurnPhase phase = TurnPhase.IDLE;
    public GameObject turnLight;

    public GameObject GTGameOver;
    public GameObject GTRoundResult;

    public Deck deck;
    public List<CardBartok> drawPile;
    public List<CardBartok> discardPile;

    public BartokLayout layout;
    public Transform layoutAnchor;


    void Awake() {
        S = this;

        turnLight = GameObject.Find("TurnLight");
        GTGameOver = GameObject.Find("GTGameOver");
        GTRoundResult = GameObject.Find("GTRoundResult");
        GTGameOver.SetActive(false);
        GTRoundResult.SetActive(false);
    }

    void Start() {
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<BartokLayout>();
        layout.ReadLayout(layoutXML.text);

        drawPile = UpgradeCardsList(deck.cards);
        LayoutGame();
    }

    private void LayoutGame() {

        if(layoutAnchor == null) {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        ArrangeDrawPile();

        Player player1;
        players = new List<Player>();

        foreach(SlotDef tSD in layout.slotDefs) {
            player1 = new Player();
            player1.handSlotDef = tSD;
            players.Add(player1);
            player1.playerNum = players.Count;
        }
        players[0].type = PlayerType.HUMAN;

        CardBartok tCB;
        for(int i = 0; i < numStartingCards; i++) {
            for(int j = 0; j < 4; j++) {
                tCB = Draw();
                tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);
                players[(j + 1) % 4].AddCard(tCB);
            }
        }

        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));

    }

    private void ArrangeDrawPile() {

        CardBartok tCB;

        for(int i = 0; i < drawPile.Count; i++) {
            tCB = drawPile[i];
            tCB.transform.parent = layoutAnchor;
            tCB.transform.localPosition = layout.drawpile.pos;
            tCB.faceUp = false;
            tCB.SetSortingLayerName(layout.drawpile.layerName);
            tCB.SetSortOrder(-i * 4);
            tCB.state = CBState.DRAWPILE;
        }

    }

    public CardBartok Draw() {
        CardBartok cd = drawPile[0];
        drawPile.RemoveAt(0);
        return cd;
    }

    private List<CardBartok> UpgradeCardsList(List<Card> lCD) {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach(Card tCD in lCD) {
            lCB.Add(tCD as CardBartok);
        }
        return lCB;
    }

    public void DrawFirstTarget() {
        CardBartok tCB = MoveToTarget(Draw());
        tCB.reportFinishTo = this.gameObject;
    }

    public CardBartok MoveToTarget(CardBartok tCB) {
        tCB.timeStart = 0;
        tCB.MoveTo(layout.discardPile.pos + Vector3.back);
        tCB.state = CBState.TOTARGET;
        tCB.faceUp = true;
        tCB.SetSortingLayerName("10");
        tCB.eventualSortLayer = layout.target.layerName;

        if(targetCard != null) {
            MoveToDiscard(targetCard);
        }

        targetCard = tCB;

        return tCB;
    }

    public CardBartok MoveToDiscard(CardBartok tCB) {
        tCB.state = CBState.DISCARD;
        discardPile.Add(tCB);
        tCB.SetSortingLayerName(layout.discardPile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

        return tCB;
    }

    public void CBCallback(CardBartok cb) {
        Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.CBCallback()", cb.name);
        StartGame();
    }

    public void StartGame() {
        PassTurn(1);
    }

    public void PassTurn(int num = -1) {

        if(num == -1) {
            int index = players.IndexOf(CURRENT_PLAYER);
            num = (index + 1) % 4;
        }

        int lasyPlayerNum = -1;
        if(CURRENT_PLAYER != null) {
            lasyPlayerNum = CURRENT_PLAYER.playerNum;
            if(CheckGameOver()) {
                return;
            }
        }

        CURRENT_PLAYER = players[num];
        phase = TurnPhase.PRE;

        CURRENT_PLAYER.TakeTurn();

        Vector3 lPos = CURRENT_PLAYER.handSlotDef.pos + Vector3.back * 5;
        turnLight.transform.position = lPos;

        Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.PassTurn()", "Old: " + lasyPlayerNum, "New:" + CURRENT_PLAYER.playerNum);

    }

    public bool ValidPlay(CardBartok cb) {
        if(cb.rank == targetCard.rank) {
            return true;
        }

        if(cb.suit == targetCard.suit) {
            return true;
        }

        return false;
    }

    public void CardClicked(CardBartok tCB) {

        if(CURRENT_PLAYER.type != PlayerType.HUMAN || phase == TurnPhase.WAITING) {
            return;
        }

        switch(tCB.state) {
            case CBState.DRAWPILE:
                CardBartok cb = CURRENT_PLAYER.AddCard(Draw());
                cb.callbackPlayer = CURRENT_PLAYER;
                Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.CardClicked()", "Draw", cb.name);
                phase = TurnPhase.WAITING;
                break;
            case CBState.HAND:
                if(ValidPlay(tCB)) {
                    CURRENT_PLAYER.RemoveCard(tCB);
                    MoveToTarget(tCB);
                    tCB.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.CardClicked()", "Play", tCB.name, targetCard.name + " is target");
                    phase = TurnPhase.WAITING;
                } else {
                    Utils.tr(Utils.RoundToPlaces(Time.time), "Bartok.CardClicked()", "Attempted to Play", tCB.name, targetCard.name + " is target");
                }
                break;
        }

    }

    public bool CheckGameOver() {

        if(drawPile.Count == 0) {
            List<Card> cards = new List<Card>();
            foreach(CardBartok cb in discardPile) {
                cards.Add(cb);
            }
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardsList(cards);
            ArrangeDrawPile();
        }

        if(CURRENT_PLAYER.hand.Count == 0) {
            if(CURRENT_PLAYER.type == PlayerType.HUMAN) {
                GTGameOver.GetComponent<GUIText>().text = "You Won!";
                GTRoundResult.GetComponent<GUIText>().text = "";
            } else {
                GTGameOver.GetComponent<GUIText>().text = "Game Over";
                GTRoundResult.GetComponent<GUIText>().text = "Player " + CURRENT_PLAYER.playerNum + " won";
            }
            GTGameOver.SetActive(true);
            GTRoundResult.SetActive(true);
            phase = TurnPhase.GAMEOVER;
            Invoke("RestartGame", 1f);

            return true;
        }

        return false;

    }

    public void RestartGame() {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Prospector_Scene_0");
    }
}
