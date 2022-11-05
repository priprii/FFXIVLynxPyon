using System.Collections.Generic;

namespace LynxPyon.Models {
    public class BlackjackProps {
        public int AceLowValue = 0;
        public int AceHighValue = 0;
        public bool Doubled = false;
        public bool DoubleHit = false;
        public bool Pushed = false;
        public bool IsPush = false;
        public List<Card> Cards = new List<Card>();

        public string GetCards() {
            string cards = "";
            foreach(Card card in Cards) {
                cards += $"{card.Text} ";
            }

            return cards.Trim();
        }

        public string GetStrValue() {
            int value = 0;
            AceLowValue = 0;
            AceHighValue = 0;

            foreach(Card card in Cards) {
                if(!card.Ace) { value += card.Value; }
            }

            List<Card> aces = Cards.FindAll(x => x.Ace);
            if(aces != null && aces.Count > 0) {
                if(value + 10 + aces.Count > 21) {
                    return $"{value + aces.Count}";
                } else {
                    AceLowValue = value + aces.Count;
                    AceHighValue = value + 10 + aces.Count;
                    return $"{value + 10 + aces.Count}";
                }
            }

            return $"{value}";
        }

        public int GetIntValue() {
            string value = GetStrValue();

            return AceHighValue != 0 ? AceHighValue : int.Parse(value);
        }
    }
}
