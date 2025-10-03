using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService
    {
        private const int ADL_MAX_PATH = 256;

        // ADL memory allocator delegate
        private delegate IntPtr ADLMainMemoryAllocator(int size);
        private static IntPtr ADL_Main_Memory_Alloc(int size) => Marshal.AllocCoTaskMem(size);

        // ADL API imports
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Create(ADLMainMemoryAllocator callback, int enumConnectedAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int inputSize);

        // Use your AdapterInfo model, not my struct
        private bool _initialized = false;

        public bool Initialize()
        {
            int result = ADL_Main_Control_Create(ADL_Main_Memory_Alloc, 1);
            if (result != 0)
            {
                OnError("ADL initialization failed with code " + result);
                return false;
            }

            _initialized = true;
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

        public IEnumerable<AdapterInfo> GetAdapters()
        {
            var adapters = new List<AdapterInfo>();

            int numAdapters = 0;
            if (ADL_Adapter_NumberOfAdapters_Get(ref numAdapters) != 0)
                return adapters;

            int structSize = Marshal.SizeOf(typeof(ADLAdapterInfoRaw));
            IntPtr buffer = Marshal.AllocCoTaskMem(structSize * numAdapters);

            if (ADL_Adapter_AdapterInfo_Get(buffer, structSize * numAdapters) == 0)
            {
                for (int i = 0; i < numAdapters; i++)
                {
                    IntPtr ptr = new IntPtr(buffer.ToInt64() + i * structSize);
                    var raw = (ADLAdapterInfoRaw)Marshal.PtrToStructure(ptr, typeof(ADLAdapterInfoRaw));

                    adapters.Add(new AdapterInfo
                    {
                        Id = raw.AdapterIndex.ToString(),
                        Name = raw.AdapterName,
                        BusNumber = raw.BusNumber
                    });
                }
            }

            Marshal.FreeCoTaskMem(buffer);
            return adapters;
        }

        public AdapterCapabilities GetCapabilities(string adapter)
        {
            // TODO: call real ADL capability functions
            return new AdapterCapabilities
            {
                SupportsAnisotropicFiltering = true,
                SupportsTessellation = true,
                SupportsVSync = true
            };
        }

        public object QuerySetting(string adapter, SettingKey settingKey)
        {
            // TODO: implement ADL queries
            return null;
        }

        public bool ApplySetting(string adapter, SettingKey settingKey, object value)
        {
            // TODO: call ADL functions to actually apply settings
            Console.WriteLine($"Would apply {settingKey}={value} to {adapter}");
            return true;
        }

        public void OnError(string message)
        {
            Console.WriteLine("ADL Error: " + message);
        }

        // Internal struct to match ADL_Adapter_AdapterInfo_Get
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct ADLAdapterInfoRaw
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
    }
}
