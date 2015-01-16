using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ShowAutoRenamer {
    class NotificationManager {
        List<Notification> notifications = new List<Notification>();
        Grid nGrid;
        Label title;
        TextBlock text;

        public NotificationManager(Grid g, Label l, TextBlock tb) {
            nGrid = g;
            title = l;
            text = tb;
            Update();
        }

        public void AddNotification(Notification n) {
            notifications.Add(n);
            Update();
        }

        public void AddNotification(string title, string text) {
            Notification n = new Notification(title, text);
            notifications.Add(n);
            Update();
        }

        public void RemoveNotification(int id = 0) {
            if(notifications.Count > id) notifications.RemoveAt(id);
            Update();
        }

        void Update() {
            if (notifications.Count == 0) { nGrid.Visibility = System.Windows.Visibility.Hidden; return; }
            else if (nGrid.Visibility == System.Windows.Visibility.Hidden) nGrid.Visibility = System.Windows.Visibility.Visible;

            title.Content = notifications[0].title; 
            text.Text = notifications[0].text;
        }

        public void DeleteSearchRelated() {
            for (int i = 0; i < notifications.Count; i++) {
                if (notifications[i].searchRelated) RemoveNotification(i--);
            }
        }

    }
}
