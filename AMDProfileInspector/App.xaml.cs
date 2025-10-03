using System;
using System.Windows;
using AMDProfileInspector.Services;

namespace AMDProfileInspector
{
    public partial class App : Application
    {
        private IAdlxService _service;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Use ADL service instead of stub
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
                MessageBox.Show("atiadlxx.dll not found. Please copy it from your AMD driver folder into the app directory.", 
                    "AMD Profile Inspector", MessageBoxButton.OK, MessageBoxImage.Error);

                // fallback so app doesn't crash
                _service = new AdlxServiceStub();
                _service.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing ADL: " + ex.Message,
                    "AMD Profile Inspector", MessageBoxButton.OK, MessageBoxImage.Error);

                // fallback so app still runs
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
