using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AMDProfileInspector.Services
{
    // Implements IAdlxService using ADL (ATI Display Library).
    // Note: ADL (atiadlxx.dll) must be present on the target machine (installed with AMD drivers).
    public class AdlService : IAdlxService
    {
        private const int ADL_MAX_PATH = 256;

        // Event required by IAdlxService
        public event Action<string> OnError;

        // Keep a mapping adapterId -> adapterIndex (ADL uses integer adapter indices)
        private readonly Dictionary<string, int> _adapterIndexMap = new();

        // ADL init flag
        private bool _initialized = false;

        // ADL memory allocator delegate
        private delegate IntPtr ADLMainMemoryAllocator(int size);
        private static IntPtr ADL_Main_Memory_Alloc(int size) => Marshal.AllocCoTaskMem(size);

        // -------- ADL (legacy) functions
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Create(ADLMainMemoryAllocator callback, int enumConnectedAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int inputSize);

        // -------- ADL2 (Application Profiles) APIs (available in modern drivers)
        // ADL2 creates a context we can pass to ADL2_* functions.
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Create(ADLMainMemoryAllocator callback, int enumConnectedAdapters, out IntPtr context);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_Main_Control_Destroy(IntPtr context);

        // ADL2 profile property set/get. The signature below is a best-effort P/Invoke.
        // Note: ADL SDK headers define exact signatures and property IDs; you may need to adapt if your driver differs.
        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_ApplicationProfiles_Property_Set(IntPtr context, int adapterIndex, [MarshalAs(UnmanagedType.LPStr)] string propertyName, int propertyType, ref int value);

        [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ADL2_ApplicationProfiles_Property_Get(IntPtr context, int adapterIndex, [MarshalAs(UnmanagedType.LPStr)] string propertyName, out int propertyType, out int value);

        // Internal ADL raw adapter struct for marshalling
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

        // ADL2 context handle (if ADL2 APIs available)
        private IntPtr _adl2Context = IntPtr.Zero;

        // Property names used with ADL2_ApplicationProfiles_Property_*.
        // These are textual property names that ADL2 may accept on modern drivers.
        // If your driver requires numeric property IDs, you'll need to replace these with the correct IDs from ADL headers.
        private const string ADL_PROP_ANISOTROPIC_ENABLE = "AnisotropicFilteringEnable"; // textual placeholder
        private const string ADL_PROP_ANISOTROPIC_LEVEL = "AnisotropicFilteringLevel";   // textual placeholder
        private const string ADL_PROP_VSYNC = "VSync";                                   // textual placeholder

        // ADL property type constants - use ADL profile property typing
        private const int ADL_PROFILEPROPERTY_TYPE_BOOLEAN = 1; // ADL_PROFILEPROPERTY_TYPE_BOOLEAN
        private const int ADL_PROFILEPROPERTY_TYPE_DWORD = 2;   // ADL_PROFILEPROPERTY_TYPE_DWORD

        // -----------------------
        // IAdlxService interface
        // -----------------------
        public bool Initialize()
        {
            try
            {
                // Try ADL2 init first (modern drivers)
                int r = ADL2_Main_Control_Create(ADL_Main_Memory_Alloc, 1, out _adl2Context);
                if (r == 0 && _adl2Context != IntPtr.Zero)
                {
                    _initialized = true;
                    // populate adapters mapping
                    BuildAdapterMap();
                    return true;
                }

                // Fallback to ADL legacy init (some drivers expose ADL but not ADL2)
                int result = ADL_Main_Control_Create(ADL_Main_Memory_Alloc, 1);
                if (result != 0)
                {
                    OnError?.Invoke($"ADL initialization failed with code {result}");
                    return false;
                }

                _initialized = true;
                BuildAdapterMap();
                return true;
            }
            catch (DllNotFoundException)
            {
                OnError?.Invoke("atiadlxx.dll not found (AMD driver missing).");
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("ADL init exception: " + ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_adl2Context != IntPtr.Zero)
                {
                    ADL2_Main_Control_Destroy(_adl2Context);
                    _adl2Context = IntPtr.Zero;
                }
                else if (_initialized)
                {
                    ADL_Main_Control_Destroy();
                }
            }
            catch { /* swallow errors on shutdown */ }

            _initialized = false;
            _adapterIndexMap.Clear();
        }

        // Build adapterId -> index mapping
        private void BuildAdapterMap()
        {
            _adapterIndexMap.Clear();

            int numAdapters = 0;
            int ret = ADL_Adapter_NumberOfAdapters_Get(ref numAdapters);
            if (ret != 0 || numAdapters <= 0) return;

            int structSize = Marshal.SizeOf(typeof(ADLAdapterInfoRaw));
            IntPtr buffer = Marshal.AllocCoTaskMem(structSize * numAdapters);
            try
            {
                if (ADL_Adapter_AdapterInfo_Get(buffer, structSize * numAdapters) == 0)
                {
                    for (int i = 0; i < numAdapters; i++)
                    {
                        IntPtr ptr = new IntPtr(buffer.ToInt64() + i * structSize);
                        var raw = (ADLAdapterInfoRaw)Marshal.PtrToStructure(ptr, typeof(ADLAdapterInfoRaw));
                        string adapterId = raw.AdapterIndex.ToString();
                        if (!_adapterIndexMap.ContainsKey(adapterId))
                            _adapterIndexMap[adapterId] = raw.AdapterIndex;
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
        }

        public IEnumerable<AdapterInfo> GetAdapters()
        {
            var list = new List<AdapterInfo>();

            int numAdapters = 0;
            int ret = ADL_Adapter_NumberOfAdapters_Get(ref numAdapters);
            if (ret != 0 || numAdapters <= 0) return list;

            int structSize = Marshal.SizeOf(typeof(ADLAdapterInfoRaw));
            IntPtr buffer = Marshal.AllocCoTaskMem(structSize * numAdapters);
            try
            {
                if (ADL_Adapter_AdapterInfo_Get(buffer, structSize * numAdapters) == 0)
                {
                    for (int i = 0; i < numAdapters; i++)
                    {
                        IntPtr ptr = new IntPtr(buffer.ToInt64() + i * structSize);
                        var raw = (ADLAdapterInfoRaw)Marshal.PtrToStructure(ptr, typeof(ADLAdapterInfoRaw));
                        var info = new AdapterInfo(raw.AdapterIndex.ToString(), "AMD", raw.AdapterName, raw.VendorID == 1002);
                        list.Add(info);

                        // ensure mapping exists
                        if (!_adapterIndexMap.ContainsKey(raw.AdapterIndex.ToString()))
                            _adapterIndexMap[raw.AdapterIndex.ToString()] = raw.AdapterIndex;
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }

            return list;
        }

        public AdapterCapabilities GetCapabilities(string adapterId)
        {
            // We assume modern ADL supports the features; a real probe would query ADL2 capability APIs.
            return new AdapterCapabilities(
                SupportsAnisotropic: true,
                SupportsTessellationControl: true,
                SupportsShaderCache: true,
                SupportsPowerProfiles: true
            );
        }

        public object QuerySetting(string adapterId, SettingKey key)
        {
            if (!_initialized)
            {
                OnError?.Invoke("ADL not initialized.");
                return null;
            }

            if (!_adapterIndexMap.TryGetValue(adapterId, out int adapterIndex))
                return null;

            try
            {
                switch (key)
                {
                    case SettingKey.AnisotropicFiltering:
                        // try ADL2 get
                        if (_adl2Context != IntPtr.Zero)
                        {
                            int type, value;
                            int r = ADL2_ApplicationProfiles_Property_Get(_adl2Context, adapterIndex, ADL_PROP_ANISOTROPIC_ENABLE, out type, out value);
                            if (r == 0) return (type == ADL_PROFILEPROPERTY_TYPE_BOOLEAN) ? (value != 0) : (object)value;
                        }
                        break;
                    case SettingKey.AnisotropicLevel:
                        if (_adl2Context != IntPtr.Zero)
                        {
                            int type, value;
                            int r = ADL2_ApplicationProfiles_Property_Get(_adl2Context, adapterIndex, ADL_PROP_ANISOTROPIC_LEVEL, out type, out value);
                            if (r == 0) return value;
                        }
                        break;
                    case SettingKey.VSync:
                        if (_adl2Context != IntPtr.Zero)
                        {
                            int type, value;
                            int r = ADL2_ApplicationProfiles_Property_Get(_adl2Context, adapterIndex, ADL_PROP_VSYNC, out type, out value);
                            if (r == 0) return (value != 0);
                        }
                        break;
                }
            }
            catch (DllNotFoundException)
            {
                OnError?.Invoke("atiadlxx.dll not found at QuerySetting time.");
            }
            catch (Exception ex)
            {
                OnError?.Invoke("QuerySetting exception: " + ex.Message);
            }

            return null;
        }

        public bool ApplySetting(string adapterId, SettingKey key, object value)
        {
            if (!_initialized)
            {
                OnError?.Invoke("ADL not initialized.");
                return false;
            }

            if (!_adapterIndexMap.TryGetValue(adapterId, out int adapterIndex))
            {
                OnError?.Invoke($"Adapter id {adapterId} not found in mapping.");
                return false;
            }

            try
            {
                // If ADL2 context exists, use profile property setter by property name.
                if (_adl2Context != IntPtr.Zero)
                {
                    switch (key)
                    {
                        case SettingKey.AnisotropicFiltering:
                            {
                                int v = Convert.ToBoolean(value) ? 1 : 0;
                                int r = ADL2_ApplicationProfiles_Property_Set(_adl2Context, adapterIndex, ADL_PROP_ANISOTROPIC_ENABLE, ADL_PROFILEPROPERTY_TYPE_BOOLEAN, ref v);
                                if (r != 0) { OnError?.Invoke($"Failed to set AnisotropicFiltering (code {r})"); return false; }
                                return true;
                            }
                        case SettingKey.AnisotropicLevel:
                            {
                                int v = Convert.ToInt32(value);
                                int r = ADL2_ApplicationProfiles_Property_Set(_adl2Context, adapterIndex, ADL_PROP_ANISOTROPIC_LEVEL, ADL_PROFILEPROPERTY_TYPE_DWORD, ref v);
                                if (r != 0) { OnError?.Invoke($"Failed to set AnisotropicLevel (code {r})"); return false; }
                                return true;
                            }
                        case SettingKey.VSync:
                            {
                                int v = Convert.ToBoolean(value) ? 1 : 0;
                                int r = ADL2_ApplicationProfiles_Property_Set(_adl2Context, adapterIndex, ADL_PROP_VSYNC, ADL_PROFILEPROPERTY_TYPE_BOOLEAN, ref v);
                                if (r != 0) { OnError?.Invoke($"Failed to set VSync (code {r})"); return false; }
                                return true;
                            }
                        default:
                            OnError?.Invoke($"ApplySetting: Unhandled key {key}");
                            return false;
                    }
                }
                else
                {
                    // ADL2 not available — implement legacy ADL behavior (if possible) or return false
                    OnError?.Invoke("ADL2 context not available; cannot apply profile-based settings on this driver.");
                    return false;
                }
            }
            catch (DllNotFoundException)
            {
                OnError?.Invoke("atiadlxx.dll not found when applying setting.");
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("ApplySetting exception: " + ex.Message);
                return false;
            }
        }
    }
}
