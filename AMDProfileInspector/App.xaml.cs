using System;
using System.Windows;
using AMDProfileInspector.Services;

namespace AMDProfileInspector
{
    public partial class App : Application
    {
        private IAdlxService _adlxService;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _adlxService = new AdlxServiceStub();
            _adlxService.Initialize();
            var mw = new MainWindow(_adlxService);
            mw.Show();
        }
    }
}
