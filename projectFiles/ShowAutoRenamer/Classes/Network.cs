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

            List<Show> sh = await Request<List<Show>>("https://api-v2launch.trakt.tv/search?query=" + forWhat + "&type=show");
            if (sh == null || sh.Count == 0) {
                NotificationManager.AddNotification(new Notification(forWhat + " was not found.", "Are you sure this is the right name for the show?", true, Importance.high));
                return null;
            }
            else
                return sh[0];
        }

        public static async Task<List<Episode>> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            List<Episode> episodes = await Request<List<Episode>>("https://api-v2launch.trakt.tv/shows/" + title + "/seasons/" + season);
            return episodes;
        }

        public static async Task<Episode> GetEpisode(Show sh, int season, int episode) {
            return await Request<Episode>("https://api-v2launch.trakt.tv/shows/" + sh.tvdb_id + "/seasons/" + season + "/episodes/" + episode);
        }

        public static async Task<Season> GetSeason(string showName, int season) {
            List<Season> list = await Request<List<Season>>("https://api-v2launch.trakt.tv/shows/" + showName + "/seasons");
            Season s = list == default(List<Season>) ? default(Season) : list.Find(x => x.season == season);
            if (s != default(Season))
                s.episodeList = await GetEpisodes(showName, s.season);
            return s;
        }
    }
}
