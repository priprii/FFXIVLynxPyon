using System.Collections.Generic;

namespace LynxPyon {
    public enum DealerStandMode { None, _16, _17 }

    public class BlackjackConfig {
        public int MaxRoll { get; set; } = 13;
        public int MinBet { get; set; } = 20000;
        public int MaxBet { get; set; } = 500000;
        public float NormalWinMultiplier { get; set; } = 2f;
        public float BlackjackWinMultiplier { get; set; } = 2.5f;
        public bool NaturalBlackjack { get; set; } = true;
        public DealerStandMode DealerStandMode { get; set; } = DealerStandMode._17;
        public bool AutoDouble { get; set; } = true;
        public bool DoubleMustHit { get; set; } = false;
        public bool PushAllowBet { get; set; } = false;
        public bool PushAllowDouble { get; set; } = true;
        public bool ShowSuit { get; set; } = true;

        public Dictionary<string, string> Messages = new Dictionary<string, string>() {
            { "PlaceBets", "Round Starting, place your bets!  #minbet# ~ #maxbet#  <se.12>" },
            { "BetsPlaced", "Bets have been placed. Good luck!! ^^ <se.12>" },

            { "PlayerBet", " #player#  bets #bet#!" },
            { "PlayerBetPushed", " #player#  bets with push of #bet#!" },
            { "PlayerBetDouble", " #player#  doubles their bet to #bet#!! ＜Stand/Hit＞" },
            { "PlayerBetDoubleMustHit", " #player#  doubles their bet to #bet#!! ＜Hit!＞" },
            { "PlayerDraw2", " #player#  draws 2 cards: #cards# (#value#)" },
            { "PlayerDraw2Blackjack", " #player#  draws 2 cards: #cards# (#value#) ★BLACKJACK★ <se.7>" },
            { "PlayerStandHit", " #player#  Hand: #cards# (#value#) ＜Stand/Hit＞" },
            { "PlayerStandHitDouble", " #player#  Hand: #cards# (#value#) ＜Stand/Hit/Double＞" },
            { "PlayerStand", " #player#  decides to stand!" },
            { "PlayerHitUnder21", " #player#  Hand: #cards# (#value#) ＜Stand/Hit＞" },
            { "PlayerHitUnder21Doubled", " #player#  Hand: #cards# (#value#) ＜Stand!＞" },
            { "PlayerHit21", " #player#  Hand: #cards# (#value#) ★BLACKJACK★ <se.7>" },
            { "PlayerHitOver21", " #player#  Hand: #cards# (#value#) BUST ;w; <se.11>" },

            { "DealerDraw1", " #dealer#  reveals 1st card: #cards#  (#value#)" },
            { "DealerDraw2UnderStand", " #dealer#  reveals 2nd card: #cards# (#value#) 《#stand# ＜Hit!＞" },
            { "DealerDraw2Stand", " #dealer#  reveals 2nd card: #cards# (#value#) 》#stand# ＜Stand!＞" },
            { "DealerDraw2NoStandReq", " #dealer#  reveals 2nd card: #cards# (#value#) ＜Stand/Hit＞" },
            { "DealerDraw2Blackjack", " #dealer#  reveals 2nd card: #cards# (#value#) ★BLACKJACK★ <se.4>" },

            { "DealerHitUnderStand", " #dealer#  Hand: #cards# (#value#) 《#stand# ＜Hit!＞" },
            { "DealerHitStand", " #dealer#  Hand: #cards# (#value#) 》#stand# ＜Stand!＞" },
            { "DealerHitNoStandReq", " #dealer#  Hand: #cards# (#value#) ＜Stand/Hit＞" },
            { "DealerHit21", " #dealer#  Hand: #cards# (#value#) ★BLACKJACK★ <se.4>" },
            { "DealerOver21", " #dealer#  Hand: #cards# (#value#) BUST ;w; <se.11>" },

            { "Win", " #player#  wins with Hand: #cards# (#value#) #winnings#!! <se.15>" },
            { "Loss", "" },
            { "Draw", " #player#  matched dealer's Hand: #cards# (#value#) #bet# ＜Push/Refund＞" },
            { "NoWinners", " #dealer#  wins this round, sorry! ^^ <se.11>" }
        };
    }
}
