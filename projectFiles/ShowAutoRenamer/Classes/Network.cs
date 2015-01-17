using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public static class Network {
        static string Request(string adress) {
            adress = adress.Replace(" ", "-");
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(adress);
            // Get the response.
            try {
                WebResponse response = request.GetResponse();
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams and the response.
                reader.Close();
                response.Close();
                return responseFromServer;
            }
            catch {
                NotificationManager.AddNotification(new Notification("The sky is falling!", "There was an error with request. If you encounter this often, please report it with show name you typed in", true));
                Debug.WriteLine(adress);
                return "";
            }
        }

        public static Show Search(string forWhat) {
            List<Show> episodes = JsonConvert.DeserializeObject<List<Show>>(Request("http://api.trakt.tv/search/shows.json/c01ff5475f1333863127ffd8816fb776?query=" + forWhat));
            NotificationManager.DeleteSearchRelated();
            if (episodes.Count == 0) return new Show();
            return episodes[0];
        }

        public static List<Episode> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            List<Episode> episodes = JsonConvert.DeserializeObject<List<Episode>>(Request("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season));
            return episodes;
        }

        /// <summary>
        /// Recommended to use since it uses caching
        /// </summary>
        /// <param name="showName"></param>
        /// <param name="season"></param>
        /// <returns></returns>
        public static Episode GetEpisode(string showName, int season, int episode) {
            episode--;
            string title = showName;
            if (title == null) return new Episode("Show not found!");
            title = title.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            List<Episode> episodes = JsonConvert.DeserializeObject<List<Episode>>(Request("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season));
            if (episodes == null) return new Episode("Show not found");
            else if (episodes.Count == 0) return new Episode();
            else if (episode >= episodes.Count) return new Episode("Found " + title + " which in season " + ++season + " has less episodes");
            return episodes[episode];
        }

        public static int GetEpisodeCount(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName))[season].episodes;
        }

        public static Season GetSeason(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName)).Find(x => x.season == season);
        }
    }
}
