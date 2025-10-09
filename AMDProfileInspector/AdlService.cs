using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService
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
            int result = ADL2_Main_Control_Create(ADL_Alloc, 1, out _context);
            _initialized = (result == 0 && _context != IntPtr.Zero);
            return _initialized;
        }

        public IEnumerable<AdapterInfo> GetAdapters()
        {
            // Minimal stub; in a full build this would query actual hardware
            return new List<AdapterInfo>
            {
                new AdapterInfo("AMDVEGA3", "AMD", "Radeon Vega 3 Graphics", true)
            };
        }

        public AdapterCapabilities GetCapabilities(string adapterId)
        {
            // Simple hardcoded support for Vega 3
            return new AdapterCapabilities(true, true, true, true);
        }

        public bool ApplySetting(string adapterId, SettingKey key, object value)
        {
            if (!_initialized)
            {
                OnError?.Invoke("ADL not initialized.");
                return false;
            }

            try
            {
                string exePath = adapterId; // for ADL, this represents the target app path
                switch (key)
                {
                    case SettingKey.AnisotropicFiltering:
                        ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "AnisotropicFiltering", Convert.ToInt32(value));
                        break;
                    case SettingKey.TextureQuality:
                        ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "TextureQuality", Convert.ToInt32(value));
                        break;
                    case SettingKey.AnisotropicLevel:
                        ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "AnisotropicLevel", Convert.ToInt32(value));
                        break;
                    default:
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Error applying setting: " + ex.Message);
                return false;
            }
        }

        public object? QuerySetting(string adapterId, SettingKey key)
        {
            // Stub — real implementation would query ADL for current value
            return null;
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
