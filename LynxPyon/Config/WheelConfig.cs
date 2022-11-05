using System.Collections.Generic;

namespace LynxPyon {
    public class WheelConfig {
        public int MinBet { get; set; } = 20000;
        public int MaxBet { get; set; } = 50000;

        public Dictionary<string, string> Messages = new Dictionary<string, string>() {
            { "WheelAd", "/yell Try your luck, spin the wheel! #minbet# - #maxbet# per Spin, /tell me how many spins you want to do & how much you want to bet! Watch live here: (Insert Stream Url)" },
            { "WheelPlay", "/yell <t> plays Spin the Wheel, #totalbet# for #spins# #spinstr#!! Watch live here: (Insert Stream Url)" },
            { "BonusChoice", "/yell <t> landed on Bonus!! Choose: Take 5x win, or play up to 5 free spins on the Bonus Wheel for chance of 10x win??" },
            { "BonusWin", "/yell <t> is super lucky!! 10x on Bonus Wheel, Congrats!!" },
            { "PrizePool", "/tell <t> Consolation Prizes: Minion, Chocolate, A little affection from one of our dancers, 20% Service Discount" }
        };
    }
}
