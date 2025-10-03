using System;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IDisposable
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

        public bool Initialize()
        {
            int result = ADL2_Main_Control_Create(ADL_Alloc, 1, out _context);
            _initialized = (result == 0 && _context != IntPtr.Zero);
            return _initialized;
        }

        public bool ApplyProfile(string exePath, string aa, string af, int lodBias, string texQuality)
        {
            if (!_initialized) return false;

            try
            {
                int aaVal = aa switch
                {
                    "Off" => 0,
                    "2x" => 2,
                    "4x" => 4,
                    "8x" => 8,
                    _ => 0
                };
                ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "AntiAliasing", aaVal);

                int afVal = af switch
                {
                    "Off" => 0,
                    "2x" => 2,
                    "4x" => 4,
                    "8x" => 8,
                    "16x" => 16,
                    _ => 0
                };
                ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "AnisotropicFiltering", afVal);

                int texVal = texQuality switch
                {
                    "High" => 0,
                    "Balanced" => 1,
                    "Performance" => 2,
                    _ => 0
                };
                ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "TextureQuality", texVal);

                ADL2_ApplicationProfiles_ProfileApplicationProperty_Set(_context, exePath, "LOD_Bias", lodBias);

                Console.WriteLine($"[ADL] Applied profile to {exePath} (AA={aa}, AF={af}, LOD={lodBias}, Tex={texQuality})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error applying profile: " + ex.Message);
                return false;
            }
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
