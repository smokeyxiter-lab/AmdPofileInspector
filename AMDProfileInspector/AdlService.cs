using System;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService  // reusing your interface
    {
        private const int ADL_MAX_PATH = 256;

        // ADL Function Delegates
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Create(ADLMainMemoryAllocator callback, int enumConnectedAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int inputSize);

        // Callback for ADL memory allocation
        private delegate IntPtr ADLMainMemoryAllocator(int size);
        private static IntPtr ADL_Main_Memory_Alloc(int size) => Marshal.AllocCoTaskMem(size);

        public void Initialize()
        {
            int result = ADL_Main_Control_Create(ADL_Main_Memory_Alloc, 1);
            if (result != 0)
                throw new Exception("ADL initialization failed with code: " + result);
        }

        public void Dispose()
        {
            ADL_Main_Control_Destroy();
        }

        public void ListAdapters()
        {
            int numAdapters = 0;
            if (ADL_Adapter_NumberOfAdapters_Get(ref numAdapters) == 0)
            {
                Console.WriteLine($"Found {numAdapters} AMD adapters.");
                // TODO: Call ADL_Adapter_AdapterInfo_Get to fetch names
            }
        }

        // Example: placeholder for real GPU setting methods
        public void ApplySetting(string adapter, string settingKey, object value)
        {
            Console.WriteLine($"Would apply {settingKey}={value} to {adapter}");
            // TODO: map settingKey to ADL functions (anisotropic, vsync, tessellation)
        }
    }
}
