using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CHVP3.Properties;
using System.Collections.ObjectModel;
using System.Threading;

namespace CHVP3
{

    public partial class LogViewer : Window
    {

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public LogViewer()
        {
            InitializeComponent();

            DataContext = LogEntries = new ObservableCollection<LogEntry>();

            AddEntry(new LogEntry("Started CHVP"));

            Timer = new Timer(x => AddEntry(GetRandomEntry()), null, 5000, 1000);
        }

        private Timer Timer;
        private Random random = new Random();

        private void AddEntry(LogEntry entry)
        {
            Dispatcher.BeginInvoke((Action)(() => LogEntries.Add(entry)));
        }

        private LogEntry GetRandomEntry()
        {
            if (random.Next(1, 10) > 1)
                return new LogEntry(string.Join(" ", "This is a random entry"));

            return new CollapsibleLogEntry(
                string.Join(" ", "This is a random entry"),
                Enumerable.Range(5, random.Next(5, 10)).Select(i => GetRandomEntry()).ToList()
            );
        }




    }

}