using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace ShowAutoRenamer {
    public static class Network {
        static Ellipse nA;

        public static void Initialize(Ellipse networkActivity) {
            nA = networkActivity;
        }

        static async Task<T> Request<T>(string adress) {
            adress = adress.Replace(" ", "-");
            try {
                using (WebClient webClient = new WebClient()) {
                    return JsonConvert.DeserializeObject<T>(await webClient.DownloadStringTaskAsync(adress));
                }
            }
            catch {
                return default(T);
            }
        }

        public static async Task<Show> Search(string forWhat) {
            NotificationManager.DeleteSearchRelated();
            List<Show> sh = await Request<List<Show>>("http://api.trakt.tv/search/shows.json/c01ff5475f1333863127ffd8816fb776?query=" + forWhat);
            if (sh == null || sh.Count == 0) {
                NotificationManager.AddNotification(new Notification(forWhat + " was not found.", "Are you sure this is the right name for the show?", true, Importance.high));
                return null;
            }
            else
                return sh[0];
        }

        public static async Task<List<Episode>> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            List<Episode> episodes = await Request<List<Episode>>("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season);
            return episodes;
        }

        /// <summary>
        /// Recommended to use since it uses caching
        /// </summary>
        /// <param name="showName"></param>
        /// <param name="season"></param>
        /// <returns></returns>
        public static async Task<Episode> GetEpisode(Show sh, int season, int episode) {
            List<Episode> episodes = await Request<List<Episode>>("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + sh.tvdb_id + "/" + season);
            if (episodes == null) return new Episode("S" + season + "E" + episode + " was not found in show " + sh.title + ". Possibly a mistake?");
            else if (episode > episodes.Count) return new Episode(sh.title + " has less episodes in S" + season + " than you requested. Is this the right show?"); //to improve

            return episodes[episode - 1];
        }

        public static int GetEpisodeCount(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request<string>("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName).Result)[season].episodes;
        }

        public static async Task<Season> GetSeason(string showName, int season) {
            List<Season> list = await Request<List<Season>>("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName);
            return list == default(List<Season>) ? default(Season) : list.Find(x => x.season == season);
        }
    }
}
