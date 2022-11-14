using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace LynxPyon.Models {
    public class Player {
        public int ID;
        public string Name = "";
        public string Alias = "";
        public int TotalBet = 0;
        public int BetPerHand = 0;
        public int Winnings = 0;
        public int TotalWinnings = 0;

        public BlackjackProps Blackjack = new BlackjackProps();

        private enum ChatNameDisplayTypes { FullName, SurnameAbbrv, ForenameAbbrv, Initials }
        private unsafe ChatNameDisplayTypes ChatNameDisplayType { get { return (ChatNameDisplayTypes)ConfigModule.Instance()->GetIntValue(ConfigOption.LogNameType); } }

        public Player(int id, string name = "", string alias = "") {
            ID = id;
            Name = name;
            Alias = alias;
        }

        public string GetAlias(NameMode nameMode) {
            return GetAlias(Name, nameMode);
        }

        public string GetAlias(string name, NameMode nameMode) {
            switch(nameMode) {
                case NameMode.First:
                    return !name.Contains(' ') ? name : name.Substring(0, name.IndexOf(" ")).Trim();
                case NameMode.Last:
                    return !name.Contains(' ') ? name : name.Substring(name.IndexOf(" ")).Trim();
            }

            return name;
        }

        public unsafe string GetNameFromDisplayType(string name) {
            if(name.Contains(' ')) {
                var displayType = ChatNameDisplayType;

                if(displayType != ChatNameDisplayTypes.FullName) {
                    string[] n = name.Split(' ');
                    switch(displayType) {
                        case ChatNameDisplayTypes.ForenameAbbrv:
                            return $"{n[0].Substring(0, 1)}. {n[1]}";
                        case ChatNameDisplayTypes.SurnameAbbrv:
                            return $"{n[0]} {n[1].Substring(0, 1)}.";
                        case ChatNameDisplayTypes.Initials:
                            return $"{n[0].Substring(0, 1)}. {n[1].Substring(0, 1)}.";
                    }
                }
            }

            return name;
        }

        public void UpdateTotalBet() {
            TotalBet = 0;
            foreach(Hand hand in Blackjack.Hands) {
                TotalBet += hand.Doubled ? BetPerHand * 2 : BetPerHand;
            }
        }

        public void Reset() {
            TotalBet = Blackjack.Pushed ? TotalBet : 0;
            BetPerHand = TotalBet;
            Winnings = 0;
            Blackjack.IsPush = Blackjack.Pushed;
            Blackjack.Pushed = false;
            Blackjack.Hands[0].AceLowValue = 0;
            Blackjack.Hands[0].AceHighValue = 0;
            Blackjack.Hands[0].Doubled = false;
            Blackjack.Hands[0].DoubleHit = false;
            Blackjack.Hands[0].Cards = new List<Card>();

            if(Blackjack.Hands.Count > 1) {
                Blackjack.Hands.RemoveAt(1);
            }
        }
    }
}
