using System;
using System.Collections.Generic;

namespace AMDProfileInspector.Services
{
    public interface IAdlxService : IDisposable
    {
        event Action<string>? OnError;

        bool Initialize();

        IEnumerable<AdapterInfo> GetAdapters();

        AdapterCapabilities GetCapabilities(string adapterId);

        bool ApplySetting(string adapterId, SettingKey key, object value);

        object? QuerySetting(string adapterId, SettingKey key);
    }
}
