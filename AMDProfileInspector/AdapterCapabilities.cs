namespace AMDProfileInspector.Services
{
    public class AdapterCapabilities
    {
        public bool SupportsAnisotropicFiltering { get; }
        public bool SupportsTextureQuality { get; }
        public bool SupportsTessellation { get; }
        public bool SupportsShaderCache { get; }

        public AdapterCapabilities(
            bool anisotropicFiltering,
            bool textureQuality,
            bool tessellation,
            bool shaderCache)
        {
            SupportsAnisotropicFiltering = anisotropicFiltering;
            SupportsTextureQuality = textureQuality;
            SupportsTessellation = tessellation;
            SupportsShaderCache = shaderCache;
        }
    }
}
