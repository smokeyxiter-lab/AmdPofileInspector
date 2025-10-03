using System;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IDisposable
    {
        // Delegate for ADL memory allocation
        public delegate IntPtr ADL_Main_Memory_Alloc(int size);

        // Import ADL functions from atiadlxx.dll
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Create(ADL_Main_Memory_Alloc callback, int enumConnectedAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        // Memory allocator for ADL
        private static IntPtr ADL_Alloc(int size)
        {
            return Marshal.AllocCoTaskMem(size);
        }

        private bool _initialized;

        public bool Initialize()
        {
            try
            {
                int result = ADL_Main_Control_Create(ADL_Alloc, 1);
                _initialized = (result == 0);
                return _initialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ADL initialization failed: " + ex.Message);
                return false;
            }
        }

        public int GetAdapterCount()
        {
            if (!_initialized) return 0;

            int num = 0;
            if (ADL_Adapter_NumberOfAdapters_Get(ref num) == 0)
                return num;

            return 0;
        }

        public bool ApplyProfile(string exePath, string aa, string af, int lodBias, string texQuality)
        {
            if (!_initialized) return false;

            // Placeholder for actual ADL profile code
            Console.WriteLine($"[ADL] Apply to {exePath}: AA={aa}, AF={af}, LOD={lodBias}, Tex={texQuality}");
            return true;
        }

        public void Dispose()
        {
            if (_initialized)
            {
                ADL_Main_Control_Destroy();
                _initialized = false;
            }
        }
    }
}
