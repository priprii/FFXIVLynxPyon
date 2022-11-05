using System;
using System.Collections.Generic;
using LynxPyon.Models;

namespace LynxPyon {
    public class LoungeConfig {
        public Dictionary<string, string> Messages = new Dictionary<string, string>() {
            { "Order", "#customer#:  #firstname#  orders #items#! 》 #totalcost#" }
        };

        public string MenuEndpoint { get; set; } = "https://emerald-experience.carrd.co/#menue";
        public string MenuStartPattern { get; set; } = "menue-section";
        public string MenuEndPattern { get; set; } = "service-section";
        public string MenuContentPattern { get; set; } = "<td>([^<]+)</td><td>([^G]+)[^<]*</td>";

        public string StaffEndpoint { get; set; } = "https://emerald-lynx-staff.carrd.co/";
        public string StaffStartPattern { get; set; } = "data-scroll-id=\"start\"";
        public string StaffEndPattern { get; set; } = "id=\"buttons01\"";
        public string StaffContentPattern { get; set; } = "<p[^>]+>([^<]+)</p><h2[^>]+>([^<]+)</h2>";

        public DateTime LastUpdatedStaff { get; set; } = DateTime.MinValue;
        public List<Staff> Staff { get; set; } = new List<Staff>();
        public DateTime LastUpdatedMenu { get; set; } = DateTime.MinValue;
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
