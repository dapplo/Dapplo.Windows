using System;

namespace Dapplo.Windows.Software
{
    /// <summary>
    /// This class contains all known fields which describe installed software
    /// </summary>
    public class SoftwareDetails
    {
        public Guid? Id { get; set; }
        public string AuthorizedCDFPrefix { get; set; }
        public string Contact { get; set; }
        public string Comments { get; set; }
        public string DisplayName { get; set; }
        public string DisplayVersion { get; set; }
        public int EstimatedSize { get; set; }
        public string HelpLink { get; set; }
        public string HelpTelephone { get; set; }
        public string InstallDate { get; set; }
        public bool SystemComponent { get; set; }
        public string InstallLocation { get; set; }
        public string InstallSource { get; set; }
        public int Language { get; set; }
        public string ModifyPath { get; set; }
        public string Publisher { get; set; }
        public string Readme { get; set; }
        public long Size { get; set; }
        public string UninstallString { get; set; }
        public string URLInfoAbout { get; set; }
        public string URLUpdateInfo { get; set; }
        public int Version { get; set; }
        public int VersionMajor { get; set; }
        public int VersionMinor { get; set; }
        public bool WindowsInstaller { get; set; }
        public bool NoModify { get; set; }
        public bool NoRepair { get; set; }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id ?? default}: {DisplayName} - {DisplayVersion}";
        }
    }
}
