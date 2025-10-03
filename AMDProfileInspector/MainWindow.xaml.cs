using System.Windows;
using AMDProfileInspector.Services;

namespace AMDProfileInspector
{
    public partial class MainWindow : Window
    {
        private readonly IAdlxService _service;
        public MainWindow(IAdlxService service)
        {
            InitializeComponent();
            _service = service;
        }

        private void btnList_Click(object sender, RoutedEventArgs e)
        {
            lstAdapters.Items.Clear();
            foreach (var a in _service.GetAdapters())
            {
                lstAdapters.Items.Add($"{a.Name} ({a.Vendor}) - Integrated: {a.IsIntegrated}");
            }
        }
    }
}
