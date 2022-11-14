using System.Collections.Generic;
using System.Linq;
using LynxPyon.Models;

namespace LynxPyon {
    public class Hand {
        public int AceLowValue = 0;
        public int AceHighValue = 0;
        public bool Doubled = false;
        public bool DoubleHit = false;
        public List<Card> Cards = new List<Card>();

        public string GetCards(bool forceOnlyValue = false) {
            return string.Join(" ", Cards.Select(c => forceOnlyValue ? c.TextNoSuit : c.Text).ToArray());
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
