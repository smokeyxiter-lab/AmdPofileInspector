namespace AMDProfileInspector.Services
{
    public class AdapterInfo
    {
        public string AdapterId { get; }
        public string Vendor { get; }
        public string Name { get; }
        public bool IsIntegrated { get; }

        public AdapterInfo(string adapterId, string vendor, string name, bool isIntegrated)
        {
            AdapterId = adapterId;
            Vendor = vendor;
            Name = name;
            IsIntegrated = isIntegrated;
        }

        public override string ToString() => $"{Name} ({Vendor})";
    }
}
