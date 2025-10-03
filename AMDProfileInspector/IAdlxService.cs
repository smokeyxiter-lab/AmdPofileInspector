using System;
using System.Collections.Generic;

namespace AMDProfileInspector.Services
{
    public enum SettingKey
    {
        AnisotropicFiltering,
        AnisotropicLevel,
        TextureQuality,
        TessellationLevel,
        ShaderCache,
        VSync,
        PowerProfile
    }

    public record AdapterInfo(string AdapterId, string Vendor, string Name, bool IsIntegrated);

    public record AdapterCapabilities(bool SupportsAnisotropic, bool SupportsTessellationControl, bool SupportsShaderCache, bool SupportsPowerProfiles);

    public interface IAdlxService : IDisposable
    {
        bool Initialize();
        IEnumerable<AdapterInfo> GetAdapters();
        AdapterCapabilities GetCapabilities(string adapterId);
        bool ApplySetting(string adapterId, SettingKey key, object value);
        object QuerySetting(string adapterId, SettingKey key);
        event Action<string> OnError;
    }
}
