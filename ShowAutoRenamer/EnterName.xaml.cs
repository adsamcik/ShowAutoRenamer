using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for EnterName.xaml
    /// </summary>
    public partial class EnterName : Window {
        public string Name;
        public string Season;
        public string Episode;
        public EnterName(string name, string season, string episode) {
            InitializeComponent();
            this.season.Text = season;
            this.episodeName.Text = name;
            this.episode.Text = episode;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Name = this.episodeName.Text;
            Season = this.season.Text;
            Episode = this.episodeName.Text;
            this.Close();
        }
    }
}
