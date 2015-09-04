using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace ShowAutoRenamer {
    public static class Network {
        public const string API_KEY = "5c241f3e48baaeb82e5f86889c570dd00c0ef2b57c8f8aae5b6143edbf0c70c3";
        static Ellipse nA;

        public static void Initialize(Ellipse networkActivity) {
            nA = networkActivity;
        }

        static async Task<string> Request(string requestString) {
            requestString = requestString.Replace(" ", "-");
            Debug.WriteLine("request url https://api-v2launch.trakt.tv/" + requestString);
            using (var httpClient = new HttpClient { BaseAddress = new Uri("https://api-v2launch.trakt.tv/") }) {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-version", "2");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-key", API_KEY);
                using (var response = await httpClient.GetAsync(requestString)) {      
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static async Task<Show> Search(string forWhat) {
            NotificationManager.DeleteSearchRelated();

            string result = await Request("search?query=" + forWhat + "&type=show");
            Show s = JsonConvert.DeserializeObject<Show>(Functions.CutFromJson(result, "show"));
            if (s == null) {
                NotificationManager.AddNotification(new Notification(forWhat + " was not found.", "Are you sure this is the right name for the show?", true, Importance.high));
                return s;
            }
            else
                return s;
        }

        public static async Task<List<Episode>> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            string episodes = await Request("shows/" + title + "/seasons/" + season);
            return JsonConvert.DeserializeObject<List<Episode>>(episodes);
        }

        public static async Task<Episode> GetEpisode(Show sh, int season, int episode) {
            return JsonConvert.DeserializeObject<Episode>(await Request("shows/" + sh.title + "/seasons/" + season + "/episodes/" + episode));
        }

        public static async Task<Season> GetSeason(Show show, int season) {
            string result = await Request("shows/" + show.title + "/seasons/" + season);
            Season s = new Season(season, show);
            s.episodeList = JsonConvert.DeserializeObject<List<Episode>>(result);
            if (s != default(Season))
                s.episodeList = await GetEpisodes(show.title, s.season);
            return s;
        }
    }
}
