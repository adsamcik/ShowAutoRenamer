using System.Windows;

namespace ShowAutoRenamer {
    /// <summary>
    /// Interaction logic for EnterName.xaml
    /// </summary>
    public partial class EnterName : Window {
        public string EpisodeName;
        public string Season;
        public string Episode;
        public EnterName(string name, string season, string episode) {
            InitializeComponent();
            this.season.Text = season;
            this.episodeName.Text = name;
            this.episode.Text = episode;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            EpisodeName = this.episodeName.Text;
            Season = this.season.Text;
            Episode = this.episodeName.Text;
            this.Close();
        }
    }
}
