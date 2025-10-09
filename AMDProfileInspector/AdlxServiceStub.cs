using System;
using System.Collections.Generic;
using System.Linq;

namespace AMDProfileInspector.Services
{
    public class AdlxServiceStub : IAdlxService
    {
        private readonly List<AdapterInfo> _adapters;
        private readonly Dictionary<(string, SettingKey), object> _store = new();

        public event Action<string>? OnError;

        public AdlxServiceStub()
        {
            _adapters = new List<AdapterInfo>
            {
                new AdapterInfo("INTEL0", "Intel", "Intel(R) UHD Graphics 620", true),
                new AdapterInfo("AMDVEGA3", "AMD", "Radeon Vega 3 (Simulated)", true)
            };
            SeedDefaults("AMDVEGA3");
        }

        private void SeedDefaults(string id)
        {
            _store[(id, SettingKey.AnisotropicFiltering)] = true;
            _store[(id, SettingKey.AnisotropicLevel)] = 8;
            _store[(id, SettingKey.TextureQuality)] = "Balanced";
            _store[(id, SettingKey.TessellationLevel)] = 2;
            _store[(id, SettingKey.ShaderCache)] = true;
            _store[(id, SettingKey.VSync)] = false;
            _store[(id, SettingKey.PowerProfile)] = "Balanced";
        }

        public bool Initialize() => true;

        public IEnumerable<AdapterInfo> GetAdapters() => _adapters.ToArray();

        public AdapterCapabilities GetCapabilities(string adapterId)
        {
            var info = _adapters.FirstOrDefault(a => a.AdapterId == adapterId);
            if (info == null)
            {
                OnError?.Invoke($"Adapter not found: {adapterId}");
                return new AdapterCapabilities(false, false, false, false);
            }

            return info.AdapterId == "AMDVEGA3"
                ? new AdapterCapabilities(true, true, true, true)
                : new AdapterCapabilities(true, false, false, false);
        }

        public bool ApplySetting(string adapterId, SettingKey key, object value)
        {
            if (!_adapters.Any(a => a.AdapterId == adapterId))
            {
                OnError?.Invoke($"Adapter not found: {adapterId}");
                return false;
            }
            _store[(adapterId, key)] = value;
            return true;
        }

        public object? QuerySetting(string adapterId, SettingKey key)
        {
            return _store.TryGetValue((adapterId, key), out var val) ? val : null;
        }

        public void Dispose() { }
    }
}
