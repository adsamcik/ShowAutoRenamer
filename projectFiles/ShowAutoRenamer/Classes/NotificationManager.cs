using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ShowAutoRenamer {
    public static class NotificationManager {
        static List<Notification> notifications = new List<Notification>();
        static Grid nGrid;
        static Label title;
        static TextBlock text;
        static Dispatcher dispatcher;

        public static void Initialize(Grid g, Label l, TextBlock tb, Dispatcher d) {
            nGrid = g;
            title = l;
            text = tb;
            dispatcher = d;
            Update();
        }

        public static void AddNotification(Notification n) {
            notifications.Add(n);
            Update();
        }

        public static void AddNotification(string title, string text) {
            Notification n = new Notification(title, text);
            notifications.Add(n);
            Update();
        }

        public static void RemoveNotification(int id = 0) {
            if (notifications.Count > id) notifications.RemoveAt(id);
            Update();
        }


        public static void DeleteSearchRelated() {
            for (int i = 0; i < notifications.Count; i++) {
                if (notifications[i].searchRelated) RemoveNotification(i--);
            }
        }

        static void Update() {
            dispatcher.Invoke((Action)(() => {
                if (notifications.Count == 0) { nGrid.Visibility = System.Windows.Visibility.Hidden; return; }
                else if (nGrid.Visibility == System.Windows.Visibility.Hidden) nGrid.Visibility = System.Windows.Visibility.Visible;

                title.Content = notifications[0].title;
                text.Text = notifications[0].text;
            }));



        }

    }
}
