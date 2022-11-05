using System.Collections.Generic;

namespace LynxPyon.Models {
    public class Order {
        public Player? Customer { get; set; }
        public List<MenuItem>? Items { get; set; } = new List<MenuItem>();

        public string GetItems() {
            if(Items == null || Items.Count == 0) { return ""; }
            string items = "";

            foreach(MenuItem item in Items) {
                items += $"{item.Quantity}x {item.Name}{(Items[^1] != item && Items.Count != 1 ? ", " : "")}";
            }

            return items;
        }

        public int GetTotalCost() {
            if(Items == null || Items.Count == 0) { return 0; }
            int totalCost = 0;

            foreach(MenuItem item in Items) {
                totalCost += item.Price * item.Quantity;
            }

            return totalCost;
        }
    }
}
