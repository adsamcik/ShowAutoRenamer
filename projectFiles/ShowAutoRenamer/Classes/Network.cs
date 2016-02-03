using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public static class Network {
        public const string API_KEY = "5c241f3e48baaeb82e5f86889c570dd00c0ef2b57c8f8aae5b6143edbf0c70c3";

        static async Task<string> Request(string requestString) {
            requestString = requestString.Replace(" ", "-");
            Debug.WriteLine("request url https://api-v2launch.trakt.tv/" + requestString);
            using (var httpClient = new HttpClient { BaseAddress = new Uri("https://api-v2launch.trakt.tv/") }) {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-version", "2");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-key", API_KEY);
                using (var response = await httpClient.GetAsync(requestString)) {
                    Debug.WriteLine(await response.Content.ReadAsStringAsync());   
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static async Task<Show> Search(string forWhat) {
            NotificationManager.DeleteSearchRelated();
            if(string.IsNullOrWhiteSpace(forWhat)) {
                NotificationManager.AddNotification(new Notification("Missing showname", "Show name could not be found in the title, please enter it manualy", true, Importance.high));
                return null;
            }

            string result = await Request("search?query=" + forWhat + "&type=show");
            if (result == "[]") {
                NotificationManager.AddNotification(new Notification(forWhat + " was not found.", "Are you sure this is the right name for the show?", true, Importance.high));
                return null;
            }
            else
                return JsonConvert.DeserializeObject<Show>(CutFromJson(result, "show"));
        }

        public static async Task<List<Episode>> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            string episodes = await Request("shows/" + title + "/seasons/" + season);
            return JsonConvert.DeserializeObject<List<Episode>>(episodes);
        }

        public static async Task<Episode> GetEpisode(Show sh, int season, int episode) {
            string result = await Request("shows/" + sh.ids.trakt + "/seasons/" + season + "/episodes/" + episode);
            if (string.IsNullOrEmpty(result))
                return null;
            Episode e = JsonConvert.DeserializeObject<Episode>(result);
            e.show = sh;
            return e;
        }

        public static async Task<Season> GetSeason(Show show, int season) {
            string result = await Request("shows/" + show.title + "/seasons/" + season);
            Season s = new Season(season, show);
            s.episodeList = JsonConvert.DeserializeObject<List<Episode>>(result);
            if (s != default(Season))
                s.episodeList = await GetEpisodes(show.title, s.season);
            return s;
        }

        public static string CutFromJson(string source, string lookForObjectName) {
            string result = source.Substring(source.IndexOf(@"""" + lookForObjectName + @""":{") + lookForObjectName.Length + 3);
            int nestedDepth = 0;
            for (int i = 0; i < result.Length; i++) {
                if (result[i] == '}') {
                    if (nestedDepth == 0) {
                        result = result.Substring(0, i);
                        break;
                    }
                    else
                        nestedDepth--;
                }
                else if (result[i] == '{') nestedDepth++;
            }
            return result;
        }
    }
}
