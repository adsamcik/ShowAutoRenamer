﻿using Newtonsoft.Json;
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

            using (WebClient webClient = new WebClient()) {
                return JsonConvert.DeserializeObject<T>(await webClient.DownloadStringTaskAsync(adress));
            }
        }

        public static async Task<Show> Search(string forWhat) {
            NotificationManager.DeleteSearchRelated();
            List<Show> sh = await Request<List<Show>>("http://api.trakt.tv/search/shows.json/c01ff5475f1333863127ffd8816fb776?query=" + forWhat);
            return (sh.Count > 0) ? sh[0] : null;
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
        public static async Task<Episode> GetEpisode(string showName, int season, int episode) {
            episode--;
            string title = showName;
            if (title == null) return new Episode("Show not found!");
            title = title.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            try {
                List<Episode> episodes = await Request<List<Episode>>("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season);

                if (episode >= episodes.Count) return new Episode("Found " + title + " which in season " + ++season + " has less episodes");

                return episodes[episode];
            }
            catch {
                return new Episode("S" + season + "E" + episode + " was not found in show " + showName + ". Possibly a mistake?");
            }
        }

        public static int GetEpisodeCount(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request<string>("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName).Result)[season].episodes;
        }

        public static async Task<Season> GetSeason(string showName, int season) {
            return (await Request<List<Season>>("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName)).Find(x => x.season == season);
        }
    }
}