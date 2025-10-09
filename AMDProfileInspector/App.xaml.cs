using System;
using System.Windows;
using AMDProfileInspector.Services;

namespace AMDProfileInspector
{
    public partial class App : Application
    {
        private IAdlxService? _service;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _service = new AdlService();

                if (!_service.Initialize())
                {
                    MessageBox.Show("Failed to initialize ADL. Falling back to stub service.",
                        "AMD Profile Inspector", MessageBoxButton.OK, MessageBoxImage.Warning);

                    _service = new AdlxServiceStub();
                    _service.Initialize();
                }
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("atiadlxx.dll not found. Copy it from your AMD driver folder into this app's directory.",
                    "AMD Profile Inspector", MessageBoxButton.OK, MessageBoxImage.Error);

                _service = new AdlxServiceStub();
                _service.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing ADL: " + ex.Message,
                    "AMD Profile Inspector", MessageBoxButton.OK, MessageBoxImage.Error);

                _service = new AdlxServiceStub();
                _service.Initialize();
            }

            var mainWindow = new MainWindow(_service);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _service?.Dispose();
            base.OnExit(e);
        }
    }
}
