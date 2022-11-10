using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Dalamud.Logging;
using LynxPyon.Models;

namespace LynxPyon {
    public class Data {
        private readonly HttpClient? HTTPClient;

        private Config Config;

        public Data(Config config) {
            Config = config;

            HTTPClient = new HttpClient {
                Timeout = TimeSpan.FromMilliseconds(8000),
            };
        }

        public void Dispose() {
            HTTPClient?.Dispose();
        }

        private async Task<HttpResponseMessage> RequestAsync(string endpoint) {
            if(HTTPClient == null) { return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); }

            return await HTTPClient.GetAsync(new Uri(endpoint));
        }

        public List<MenuItem> GetMenuItems() {
            HttpResponseMessage result;
            try {
                result = RequestAsync(Config.Lounge.MenuEndpoint).Result;
            } catch(Exception ex) {
                PluginLog.Error($"GetMenuItems Failed, Error: {ex}");
                return new List<MenuItem>();
            }

            if(result.StatusCode != HttpStatusCode.OK) {
                PluginLog.Error($"GetMenuItems Failed, Status: {result.StatusCode}");
                return new List<MenuItem>();
            }

            return ParseMenuItems(HttpUtility.HtmlDecode(result.Content.ReadAsStringAsync().Result));
        }

        private List<MenuItem> ParseMenuItems(string content) {
            List<MenuItem> menuItems = new List<MenuItem>();

            content = content.Substring(content.IndexOf(Config.Lounge.MenuStartPattern), content.IndexOf(Config.Lounge.MenuEndPattern) - content.IndexOf(Config.Lounge.MenuStartPattern));
            MatchCollection matches = Regex.Matches(content, Config.Lounge.MenuContentPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            foreach(Match match in matches) {
                string item = match.Groups[1].Value.Trim();
                int.TryParse(match.Groups[2].Value.Trim(), out int price);

                menuItems.Add(new MenuItem(item, price));
            }

            menuItems.Sort((x, y) => x.Name.CompareTo(y.Name));

            return menuItems;
        }

        public List<Staff> GetStaff() {
            HttpResponseMessage result;
            try {
                result = result = RequestAsync(Config.Lounge.StaffEndpoint).Result;
            } catch(Exception ex) {
                PluginLog.Error($"GetStaff Failed, Error: {ex}");
                return new List<Staff>();
            }

            if(result.StatusCode != HttpStatusCode.OK) {
                PluginLog.Error($"GetStaff Failed, Status: {result.StatusCode}");
                return new List<Staff>();
            }

            return ParseStaff(HttpUtility.HtmlDecode(result.Content.ReadAsStringAsync().Result));
        }

        private List<Staff> ParseStaff(string content) {
            List<Staff> staff = new List<Staff>();

            content = content.Substring(content.IndexOf(Config.Lounge.StaffStartPattern), content.IndexOf(Config.Lounge.StaffEndPattern) - content.IndexOf(Config.Lounge.StaffStartPattern));
            MatchCollection matches = Regex.Matches(content, Config.Lounge.StaffContentPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach(Match match in matches) {
                string job = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();

                if(!name.ToLower().Contains("for hire") && !name.ToLower().Contains("hiring") && !name.ToLower().Contains("comming soon")) {
                    staff.Add(new Staff(name, job));
                }
            }

            staff.Sort((x, y) => x.Name.CompareTo(y.Name));

            return staff;
        }
    }
}
