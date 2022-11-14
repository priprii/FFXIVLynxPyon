using System.Collections.Generic;
using System.Linq;

namespace LynxPyon.Models {
    public class BlackjackProps {
        public List<Hand> Hands = new List<Hand>() { new Hand() };
        public bool Pushed = false;
        public bool IsPush = false;

        public string GetCards() {
            return string.Join(" & ", Hands.Select(h => h.GetCards()).ToArray());
        }

        public string GetValues() {
            return string.Join(" & ", Hands.Select(h => h.AceLowValue != 0 ? (h.AceHighValue < 21 ? $"{h.AceLowValue}/{h.AceHighValue}" : h.AceHighValue == 21 ? $"{h.AceHighValue}" : $"{h.AceLowValue}") : h.GetStrValue()).ToArray());
        }
    }
}
