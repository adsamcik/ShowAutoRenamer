using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShowAutoRenamer {
    /// <summary>
    /// Interaction logic for TitleRegexWindow.xaml
    /// </summary>
    public partial class TitleRegexWindow : Window {
        Episode e;
        int episodeAdd, seasonAdd;

        public TitleRegexWindow() {
            InitializeComponent();
        }

        public void Initialize(Episode e) {
            this.e = e;
            this.e.show = new Show("Futurama");
            this.e.title = "Episode title";
            textBoxTitleRegex_TextChanged(null, null);
        }

        public void GetResults(out string regex, out int episodeAdd, out int seasonAdd) {
            regex = textBoxTitleRegex.Text;
            episodeAdd = this.episodeAdd;
            seasonAdd = this.seasonAdd;
        }

        private void DoneButtonClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        private void textBoxTitleRegex_TextChanged(object sender, TextChangedEventArgs e) {
            if (IsLoaded && !string.IsNullOrEmpty(textBlockTitlePreview.Text)) {
                if (this.e == null)
                    this.e = new Episode("Episode name", 1, 1, new ShowAutoRenamer.Show("Show name"));
                string title = textBoxTitleRegex.Text;
                title = Regex.Replace(title, "{title}", this.e.title, RegexOptions.IgnoreCase);
                title = Regex.Replace(title, "{showname}", this.e.show.title, RegexOptions.IgnoreCase);
                if (!DetectSubAddRegex(ref title, "season", this.e.season, out seasonAdd))
                    title = Regex.Replace(title, "{season}", this.e.season.ToString(), RegexOptions.IgnoreCase);
                if (!DetectSubAddRegex(ref title, "episode", this.e.number, out episodeAdd))
                    title = Regex.Replace(title, "{episode}", this.e.number.ToString(), RegexOptions.IgnoreCase);

                textBlockTitlePreview.Text = title;
            }
        }

        bool DetectSubAddRegex(ref string text, string before, int beforeVal, out int addValue) {
            Match m;
            if ((m = Regex.Match(text, "{" + before)).Success) {
                if (text[m.Index + m.Length] == '-' || text[m.Index + m.Length] == '+') {
                    int startAt = m.Index + m.Length;
                    int index = text.IndexOf('}', startAt);
                    if (index >= 0) {
                        int result;
                        if (int.TryParse(text.Substring(startAt, index - startAt), out result)) {
                            text = Regex.Replace(text, "{" + before + "[\\+\\-0-9]+}", (beforeVal + result).ToString(), RegexOptions.IgnoreCase);
                            addValue = result;
                            return true;
                        }
                    }
                }
            }
            addValue = 0;
            return false;
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }
    }
}
