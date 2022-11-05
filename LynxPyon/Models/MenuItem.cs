namespace LynxPyon.Models {
    public class MenuItem {
        public string Name { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }

        public MenuItem(string name, int price, int quantity = 0) {
            Name = name;
            Price = price;
            Quantity = quantity;
        }
    }
}
