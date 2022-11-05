using System.Collections.Generic;

namespace LynxPyon.Models {
    public class Player {
        public int ID;
        public string Name = "";
        public string Alias = "";
        public int Bet = 0;
        public int Winnings = 0;
        public int TotalWinnings = 0;
        
        public BlackjackProps Blackjack = new BlackjackProps();

        public Player(int id, string name = "", string alias = "") {
            ID = id;
            Name = name;
            Alias = alias;
        }

        public string GetName(NameMode nameMode) {
            switch(nameMode) {
                case NameMode.First:
                    return !Name.Contains(' ') ? Name : Name.Substring(0, Name.IndexOf(" ")).Trim();
                case NameMode.Last:
                    return !Name.Contains(' ') ? Name : Name.Substring(Name.IndexOf(" ")).Trim();
            }

            return Name;
        }

        public void Reset() {
            Bet = Blackjack.Pushed ? Bet : 0;
            Winnings = 0;
            Blackjack.AceLowValue = 0;
            Blackjack.AceHighValue = 0;
            Blackjack.Doubled = false;
            Blackjack.DoubleHit = false;
            Blackjack.IsPush = Blackjack.Pushed;
            Blackjack.Pushed = false;
            Blackjack.Cards = new List<Card>();
        }
    }
}
