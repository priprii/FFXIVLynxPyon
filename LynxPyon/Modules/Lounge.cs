using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using LynxPyon.Extensions;
using LynxPyon.Models;

namespace LynxPyon.Modules {
    public class Lounge {
        private readonly MainWindow MainWindow;
        public event EventHandler<EventArgs> Close_Window;
        public bool Enabled = false;

        private Config Config;
        private Data Data;

        private SubTab CurrentSubTab = SubTab.Menu;
        private enum SubTab { Menu, Staff, Maid, Config }

        public List<Player> Customers = new List<Player>();

        private Player? SelectedCustomer;
        private List<Order> Orders = new List<Order>();
        private Order? CurrentOrder;

        public Lounge(MainWindow mainWindow) { 
            MainWindow = mainWindow;
            Config = MainWindow.Config;
            Close_Window += MainWindow.Close_Window;

            Data = new Data(Config);
            if(Config.Lounge.MenuItems.Count == 0) {
                UpdateMenuItems();
            }
            if(Config.Lounge.Staff.Count == 0) {
                UpdateStaff();
            }
        }

        public void Dispose() {
            if(Data != null) { Data.Dispose(); }
            Close_Window -= MainWindow.Close_Window;
        }

        private void UpdateMenuItems() {
            Config.Lounge.MenuItems = Data.GetMenuItems();
            Config.Lounge.LastUpdatedMenu = DateTime.Now;
            Config.Save();
        }

        private void UpdateStaff() {
            Config.Lounge.Staff = Data.GetStaff();
            Config.Lounge.LastUpdatedStaff = DateTime.Now;
            Config.Save();
        }

        private string FormatMessage(string message, Order order) {
            if(string.IsNullOrWhiteSpace(message)) { return ""; }

            return message.Replace("#customer#", order.Customer?.Name)
                .Replace("#firstname#", order.Customer?.GetName(NameMode.First))
                .Replace("#lastname#", order.Customer?.GetName(NameMode.Last))
                .Replace("#items#", order.GetItems())
                .Replace("#totalcost#", order.GetTotalCost().ToString("N0"));
        }

        public void DrawSubTabs() {
            if(ImGui.BeginTabBar("LoungeSubTabBar", ImGuiTabBarFlags.NoTooltip)) {
                if(ImGui.BeginTabItem("Menu###GamblePyon_LoungeMenu_SubTab")) {
                    CurrentSubTab = SubTab.Menu;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Staff###GamblePyon_LoungeStaff_SubTab")) {
                    CurrentSubTab = SubTab.Staff;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Maid###GamblePyon_LoungeMaid_SubTab")) {
                    CurrentSubTab = SubTab.Maid;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Config###GamblePyon_LoungeConfig_SubTab")) {
                    CurrentSubTab = SubTab.Config;
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.Spacing();
            }

            switch(CurrentSubTab) {
                case SubTab.Menu: {
                        DrawMenu();
                        break;
                    }
                case SubTab.Staff: {
                        DrawStaff();
                        break;
                    }
                case SubTab.Maid: {
                        DrawMaid();
                        break;
                    }
                case SubTab.Config: {
                        DrawConfig();
                        break;
                    }
                default:
                    DrawMenu();
                    break;
            }
        }

        private void DrawMenu() {
            DrawMenuMain();

            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawMenuMain() {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 90 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 300 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Checkbox("Enabled", ref Enabled);
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Must first enable this option to keep list of customers updated.\nShould disable it while not working in lounge to prevent unnecessary object monitoring."); }
            ImGui.NextColumn();
            if(Customers.Count > 0) {
                string selectedCustomer = SelectedCustomer == null ? "Select Customer" : $"{SelectedCustomer.Name}";

                if(ImGui.BeginCombo("###customersCombo", selectedCustomer)) {
                    if(ImGui.Selectable("Select Customer", SelectedCustomer == null)) {
                        SelectedCustomer = null;
                        CurrentOrder = null;
                        foreach(MenuItem item in Config.Lounge.MenuItems) {
                            item.Quantity = 0;
                        }
                    }

                    foreach(Player customer in Customers) {
                        if(ImGui.Selectable($"{customer.Name}", SelectedCustomer == customer)) {
                            SelectedCustomer = customer;
                            Order? currentOrder = Orders.Find(x => x.Customer == SelectedCustomer);
                            if(currentOrder == null) {
                                currentOrder = new Order() { Customer = SelectedCustomer };
                                Orders.Add(currentOrder);
                            }
                            CurrentOrder = currentOrder;
                            foreach(MenuItem item in Config.Lounge.MenuItems) {
                                MenuItem? orderItem = CurrentOrder.Items?.Find(x => x.Name == item.Name);
                                item.Quantity = orderItem == null ? 0 : orderItem.Quantity;
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
                if(CurrentOrder != null) {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-1);
                    if(ImGui.Button("Remove Order")) {
                        Orders.Remove(CurrentOrder);
                        CurrentOrder = null;
                        SelectedCustomer = null;
                    }
                    if(ImGui.IsItemHovered()) {
                        ImGui.SetTooltip($"You should press this when you're done making the order!\n..or don't, just here to keep things organized.");
                    }
                }

                ImGui.Separator();
                ImGui.Columns(1);
                ImGui.LabelText("###currentOrdersLbl", "Current Orders");
                string ordersText = "";
                foreach(Order order in Orders) {
                    if(order.Items == null || order.Items.Count == 0) { continue; }

                    ordersText += $"{FormatMessage(Config.Lounge.Messages["Order"], order)}\n";
                }
                ImGui.InputTextMultiline("###currentOrdersTxt", ref ordersText, 10000, new Vector2(550, 60));

                if(SelectedCustomer != null) {
                    ImGui.Separator();

                    ImGui.Columns(3);
                    ImGui.SetColumnWidth(0, 180 + 5 * ImGuiHelpers.GlobalScale); //Item Name
                    ImGui.SetColumnWidth(1, 70 + 5 * ImGuiHelpers.GlobalScale); //Price
                    ImGui.SetColumnWidth(2, 90 + 5 * ImGuiHelpers.GlobalScale); //Quantity

                    ImGui.Text("Item Name");
                    ImGui.SameLine();
                    if(ImGui.Button("Update###updateItems")) {
                        UpdateMenuItems();
                    }
                    if(ImGui.IsItemHovered()) {
                        ImGui.SetTooltip($"Last Update: {Config.Lounge.LastUpdatedMenu}");
                    }
                    ImGui.NextColumn();
                    ImGui.Text($"Price");
                    ImGui.NextColumn();
                    ImGui.Text("Quantity");
                    ImGui.NextColumn();

                    if(Config.Lounge.MenuItems.Count > 0) {
                        foreach(MenuItem item in Config.Lounge.MenuItems) {
                            ImGui.Separator();
                            ImGui.PushID($"item{item.Name.Replace(" ", "")}");

                            //Item Name
                            ImGui.SetNextItemWidth(-1);
                            ImGui.LabelText("###itemNameLbl", item.Name);
                            if(ImGui.IsItemHovered()) { 
                                ImGui.SetTooltip(""); //todo: On hover, show real item name
                            }
                            ImGui.NextColumn();

                            //Item Price
                            ImGui.SetNextItemWidth(-1);
                            ImGui.LabelText("###itemPriceLbl", $"{item.Price.ToString("N0")}");
                            ImGui.NextColumn();

                            //Item Quantity
                            ImGui.SetNextItemWidth(-1);
                            ImGuiEx.InputInt("###itemQuantity", item, nameof(item.Quantity));
                            ImGui.NextColumn();

                            if(CurrentOrder != null) {
                                MenuItem? orderItem = CurrentOrder.Items?.Find(x => x.Name == item.Name);

                                if(item.Quantity > 0) {
                                    if(orderItem == null) {
                                        CurrentOrder.Items?.Add(new MenuItem(item.Name, item.Price, item.Quantity));
                                    } else {
                                        orderItem.Quantity = item.Quantity;
                                    }
                                } else if(orderItem != null) {
                                    CurrentOrder.Items?.Remove(orderItem);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawStaff() {
            DrawStaffMain();

            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawStaffMain() {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 200 + 5 * ImGuiHelpers.GlobalScale); //Name
            ImGui.SetColumnWidth(1, 200 + 5 * ImGuiHelpers.GlobalScale); //Job

            ImGui.Text("Name");
            ImGui.SameLine();
            if(ImGui.Button("Update###updateStaff")) {
                UpdateStaff();
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip($"Last Update: {Config.Lounge.LastUpdatedStaff}");
            }
            ImGui.NextColumn();
            ImGui.Text($"Job");
            ImGui.NextColumn();

            if(Config.Lounge.Staff.Count > 0) {
                foreach(Staff staff in Config.Lounge.Staff) {
                    ImGui.Separator();
                    ImGui.PushID($"staff{staff.Name.Replace(" ", "")}");

                    //Name
                    ImGui.SetNextItemWidth(-1);
                    ImGui.LabelText("###staffNameLbl", staff.Name);
                    ImGui.NextColumn();

                    //Job
                    ImGui.SetNextItemWidth(-1);
                    ImGui.LabelText("###staffJobLbl", $"{staff.Job}");
                    ImGui.NextColumn();
                }
            }
        }

        private void DrawMaid() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Nothing here at the moment, sorry ;w;");
        }

        private void DrawConfig() {
            DrawConfigDataSetup();

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            DrawConfigMessages();

            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Save")) {
                Config.Save();
            }
            ImGui.SameLine();
            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawConfigDataSetup() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Menu Data Endpoint");

            ImGui.Columns(1);
            ImGuiEx.InputText("###menuEndpoint", Config.Lounge, nameof(Config.Lounge.MenuEndpoint), 500);
            DontTouch();

            ImGui.Separator();

            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(2, 200 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Start Pattern");
            ImGui.NextColumn();
            ImGui.Text("End Pattern");
            ImGui.NextColumn();
            ImGui.Text("Content Pattern");
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###menuStartPattern", Config.Lounge, nameof(Config.Lounge.MenuStartPattern));
            DontTouch();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###menuEndPattern", Config.Lounge, nameof(Config.Lounge.MenuEndPattern));
            DontTouch();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###menuContentPattern", Config.Lounge, nameof(Config.Lounge.MenuContentPattern));
            DontTouch();
            ImGui.NextColumn();


            ImGui.Separator();


            ImGui.TextColored(ImGuiColors.DalamudGrey, "Staff Data Endpoint");

            ImGui.Columns(1);
            ImGuiEx.InputText("###staffEndpoint", Config.Lounge, nameof(Config.Lounge.StaffEndpoint), 500);
            DontTouch();

            ImGui.Separator();

            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(2, 200 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Start Pattern");
            ImGui.NextColumn();
            ImGui.Text("End Pattern");
            ImGui.NextColumn();
            ImGui.Text("Content Pattern");
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###staffStartPattern", Config.Lounge, nameof(Config.Lounge.StaffStartPattern));
            DontTouch();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###staffEndPattern", Config.Lounge, nameof(Config.Lounge.StaffEndPattern));
            DontTouch();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###staffContentPattern", Config.Lounge, nameof(Config.Lounge.StaffContentPattern));
            DontTouch();
            ImGui.NextColumn();
        }

        private void DontTouch() {
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("You'll break everything if you touch this!! o((>ω< ))o\nIt's just here to test your temptations\n..and if it breaks on its own, Pyon might be able to help."); }
        }

        private void DrawConfigMessages() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Localization");
            ImGui.TextWrapped("Adjust text to be output for various events.\nKeywords: #customer#, #firstname#, #lastname#, #items#, #totalcost#");

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 180 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 500 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Descriptor");
            ImGui.NextColumn();
            ImGui.Text("Text");
            ImGui.NextColumn();

            ImGui.Separator();

            foreach(var message in Config.Lounge.Messages) {
                ImGui.PushID($"m_{message.Key}");
                ImGui.SetNextItemWidth(-1);
                ImGui.Text(message.Key);
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(-1);
                string msg = message.Value;
                ImGui.InputText($"###m{message.Key}", ref msg, 255);
                Config.Lounge.Messages[message.Key] = msg;
                ImGui.NextColumn(); ImGui.Separator();
            }
        }
    }
}
