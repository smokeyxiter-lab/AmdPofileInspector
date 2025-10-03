
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService
    {
        private const int ADL_MAX_PATH = 256;

        // Delegate for ADL memory allocation
        private delegate IntPtr ADLMainMemoryAllocator(int size);
        private static IntPtr ADL_Main_Memory_Alloc(int size) => Marshal.AllocCoTaskMem(size);

        // ADL API imports
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Create(ADLMainMemoryAllocator callback, int enumConnectedAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct AdapterInfo
        {
            public int Size;
            public int AdapterIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string UDID;
            public int BusNumber;
            public int DeviceNumber;
            public int FunctionNumber;
            public int VendorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string AdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DisplayName;
            public int Present;
            public int Exist;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPathExt;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string PNPString;
            public int OSDisplayIndex;
        }

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int inputSize);

        private bool _initialized = false;
        private List<string> _adapters = new List<string>();

        // -------------------------
        // IAdlxService implementation
        // -------------------------

        public bool Initialize()
        {
            int result = ADL_Main_Control_Create(ADL_Main_Memory_Alloc, 1);
            if (result != 0)
                return false;

            _initialized = true;
            _adapters = GetAdapters();
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

        public List<string> GetAdapters()
        {
            var adapters = new List<string>();

            int numAdapters = 0;
            if (ADL_Adapter_NumberOfAdapters_Get(ref numAdapters) != 0)
                return adapters;

            int structSize = Marshal.SizeOf(typeof(AdapterInfo));
            IntPtr buffer = Marshal.AllocCoTaskMem(structSize * numAdapters);

            if (ADL_Adapter_AdapterInfo_Get(buffer, structSize * numAdapters) == 0)
            {
                for (int i = 0; i < numAdapters; i++)
                {
                    IntPtr ptr = new IntPtr(buffer.ToInt64() + i * structSize);
                    AdapterInfo info = (AdapterInfo)Marshal.PtrToStructure(ptr, typeof(AdapterInfo));
                    adapters.Add(info.AdapterName);
                }
            }

            Marshal.FreeCoTaskMem(buffer);
            return adapters;
        }

        public Dictionary<SettingKey, object> GetCapabilities(string adapter)
        {
            // TODO: query ADL for supported settings (anisotropic, vsync, tessellation, etc.)
            return new Dictionary<SettingKey, object>
            {
                { SettingKey.AnisotropicFiltering, "Supported" },
                { SettingKey.Tessellation, "Supported" },
                { SettingKey.VSync, "Supported" }
            };
        }

        public object QuerySetting(string adapter, SettingKey settingKey)
        {
            // TODO: implement querying the driver
            return null;
        }

        public bool ApplySetting(string adapter, SettingKey settingKey, object value)
        {
            // TODO: call real ADL functions here
            Console.WriteLine($"Would apply {settingKey}={value} to {adapter}");
            return true;
        }

        public void OnError(string message)
        {
            Console.WriteLine("ADL Error: " + message);
        }
    }
}
