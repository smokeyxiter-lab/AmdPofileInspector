using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    public class AdlService : IAdlxService
    {
        private const int ADL_MAX_PATH = 256;

        // Event required by IAdlxService
        public event Action<string> OnError;

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

        private bool _initialized = false;

        // Internal struct to marshal ADL info
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

        public bool Initialize()
        {
            int result = ADL_Main_Control_Create(ADL_Main_Memory_Alloc, 1);
            if (result != 0)
            {
                OnError?.Invoke($"ADL initialization failed with code {result}");
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

                    adapters.Add(new AdapterInfo(
                        raw.AdapterIndex.ToString(),
                        "AMD",
                        raw.AdapterName,
                        raw.VendorID == 1002 // 1002 is AMD vendor ID
                    ));
                }
            }

            Marshal.FreeCoTaskMem(buffer);
            return adapters;
        }

        public AdapterCapabilities GetCapabilities(string adapterId)
        {
            // TODO: detect real support via ADL
            return new AdapterCapabilities(
                SupportsAnisotropic: true,
                SupportsTessellationControl: true,
                SupportsShaderCache: true,
                SupportsPowerProfiles: true
            );
        }

        public bool ApplySetting(string adapterId, SettingKey key, object value)
        {
            // TODO: Implement real ADL calls here
            Console.WriteLine($"[ADL] Would apply {key}={value} to adapter {adapterId}");
            return true;
        }

        public object QuerySetting(string adapterId, SettingKey key)
        {
            // TODO: Implement ADL getter
            return null;
        }
    }
}

