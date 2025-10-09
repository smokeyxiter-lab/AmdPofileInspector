using System;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService, IDisposable
    {
        public delegate IntPtr ADL_Main_Memory_Alloc(int size);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Create(ADL_Main_Memory_Alloc callback, int enumConnectedAdapters, out IntPtr context);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Destroy(IntPtr context);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(
            IntPtr context,
            [MarshalAs(UnmanagedType.LPStr)] string appName,
            [MarshalAs(UnmanagedType.LPStr)] string propertyName,
            int value);

        private static IntPtr ADL_Alloc(int size) => Marshal.AllocCoTaskMem(size);

        private bool _initialized;
        private IntPtr _context;

        public event Action<string>? OnError;

        public bool Initialize()
        {
            try
            {
                int result = ADL2_Main_Control_Create(ADL_Alloc, 1, out _context);
                _initialized = (result == 0 && _context != IntPtr.Zero);
                return _initialized;
            }
            catch (DllNotFoundException)
            {
                OnError?.Invoke("atiadlxx.dll not found.");
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"ADL init error: {ex.Message}");
                return false;
            }
        }

        public bool ApplySetting(string adapterId, SettingKey key, object value)
        {
            if (!_initialized) return false;
            try
            {
                ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, adapterId, key.ToString(), Convert.ToInt32(value));
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error applying {key}: {ex.Message}");
                return false;
            }
        }

        public object? QuerySetting(string adapterId, SettingKey key)
        {
            // Not implemented — ADL querying would go here.
            return null;
        }

        public IEnumerable<AdapterInfo> GetAdapters()
        {
            // Simplified — in a real ADL integration, this would query the driver.
            return new[]
            {
                new AdapterInfo("AMD_ADL_0", "AMD", "Radeon (via ADL)", false)
            };
        }

        public AdapterCapabilities GetCapabilities(string adapterId)
        {
            return new AdapterCapabilities(true, true, true, true);
        }

        public void Dispose()
        {
            if (_initialized)
            {
                ADL2_Main_Control_Destroy(_context);
                _initialized = false;
            }
        }
    }
}
