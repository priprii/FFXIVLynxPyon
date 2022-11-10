namespace LynxPyon.Models {
    public class Staff {
        public string Name { get; set; } = "";
        public string Job { get; set; } = "";

        public Staff(string name, string job) {
            Name = name;
            Job = job;
        }
    }
}
