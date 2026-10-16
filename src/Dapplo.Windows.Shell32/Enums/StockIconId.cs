// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
///     The Windows stock icons, the shell's own icons for error, warning, information, shield, folders, drives and more (SHSTOCKICONID).
///     See <a href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid">SHSTOCKICONID enumeration</a>.
///     The numbering has gaps (14, 18-21, 24-27, 33-39, 41-46, 48, 107-131 and 141-180 aren't named), the values match shellapi.h of the Windows SDK.
/// </summary>
public enum StockIconId
{
    /// <summary>An invalid value (SIID_INVALID), not an icon</summary>
    Invalid = -1,
    /// <summary>Document of a type with no associated application (SIID_DOCNOASSOC)</summary>
    DocumentNoAssociation = 0,
    /// <summary>Document of a type with an associated application (SIID_DOCASSOC)</summary>
    DocumentAssociated = 1,
    /// <summary>Generic application with no custom icon (SIID_APPLICATION)</summary>
    Application = 2,
    /// <summary>Folder, generic and unspecified state (SIID_FOLDER)</summary>
    Folder = 3,
    /// <summary>Folder, open (SIID_FOLDEROPEN)</summary>
    FolderOpen = 4,
    /// <summary>5.25-inch disk drive (SIID_DRIVE525)</summary>
    Drive525 = 5,
    /// <summary>3.5-inch disk drive (SIID_DRIVE35)</summary>
    Drive35 = 6,
    /// <summary>Removable drive (SIID_DRIVEREMOVE)</summary>
    DriveRemovable = 7,
    /// <summary>Fixed drive, hard disk (SIID_DRIVEFIXED)</summary>
    DriveFixed = 8,
    /// <summary>Network drive, connected (SIID_DRIVENET)</summary>
    DriveNetwork = 9,
    /// <summary>Network drive, disconnected (SIID_DRIVENETDISABLED)</summary>
    DriveNetworkDisabled = 10,
    /// <summary>CD drive (SIID_DRIVECD)</summary>
    DriveCd = 11,
    /// <summary>RAM disk drive (SIID_DRIVERAM)</summary>
    DriveRam = 12,
    /// <summary>The entire network (SIID_WORLD)</summary>
    World = 13,
    /// <summary>A computer on the network (SIID_SERVER)</summary>
    Server = 15,
    /// <summary>A local printer or print destination (SIID_PRINTER)</summary>
    Printer = 16,
    /// <summary>The Network virtual folder (SIID_MYNETWORK)</summary>
    MyNetwork = 17,
    /// <summary>The Search feature (SIID_FIND)</summary>
    Find = 22,
    /// <summary>The Help and Support feature (SIID_HELP)</summary>
    Help = 23,
    /// <summary>Overlay for a shared item (SIID_SHARE)</summary>
    Share = 28,
    /// <summary>Overlay for a shortcut (SIID_LINK)</summary>
    Link = 29,
    /// <summary>Overlay for items that are expected to be slow to access (SIID_SLOWFILE)</summary>
    SlowFile = 30,
    /// <summary>The Recycle Bin, empty (SIID_RECYCLER)</summary>
    Recycler = 31,
    /// <summary>The Recycle Bin, not empty (SIID_RECYCLERFULL)</summary>
    RecyclerFull = 32,
    /// <summary>Audio CD media (SIID_MEDIACDAUDIO)</summary>
    MediaCdAudio = 40,
    /// <summary>Security lock (SIID_LOCK)</summary>
    Lock = 47,
    /// <summary>A virtual folder that contains the results of a search (SIID_AUTOLIST)</summary>
    AutoList = 49,
    /// <summary>A network printer (SIID_PRINTERNET)</summary>
    PrinterNetwork = 50,
    /// <summary>A server shared on a network (SIID_SERVERSHARE)</summary>
    ServerShare = 51,
    /// <summary>A local fax printer (SIID_PRINTERFAX)</summary>
    PrinterFax = 52,
    /// <summary>A network fax printer (SIID_PRINTERFAXNET)</summary>
    PrinterFaxNetwork = 53,
    /// <summary>A file that receives the output of a Print to file operation (SIID_PRINTERFILE)</summary>
    PrinterFile = 54,
    /// <summary>A category that results from a Stack by command (SIID_STACK)</summary>
    Stack = 55,
    /// <summary>Super Video CD (SVCD) media (SIID_MEDIASVCD)</summary>
    MediaSvcd = 56,
    /// <summary>A folder that contains only subfolders as child items (SIID_STUFFEDFOLDER)</summary>
    StuffedFolder = 57,
    /// <summary>Unknown drive type (SIID_DRIVEUNKNOWN)</summary>
    DriveUnknown = 58,
    /// <summary>DVD drive (SIID_DRIVEDVD)</summary>
    DriveDvd = 59,
    /// <summary>DVD media (SIID_MEDIADVD)</summary>
    MediaDvd = 60,
    /// <summary>DVD-RAM media (SIID_MEDIADVDRAM)</summary>
    MediaDvdRam = 61,
    /// <summary>DVD-RW media (SIID_MEDIADVDRW)</summary>
    MediaDvdRw = 62,
    /// <summary>DVD-R media (SIID_MEDIADVDR)</summary>
    MediaDvdR = 63,
    /// <summary>DVD-ROM media (SIID_MEDIADVDROM)</summary>
    MediaDvdRom = 64,
    /// <summary>CD+ (enhanced audio CD) media (SIID_MEDIACDAUDIOPLUS)</summary>
    MediaCdAudioPlus = 65,
    /// <summary>CD-RW media (SIID_MEDIACDRW)</summary>
    MediaCdRw = 66,
    /// <summary>CD-R media (SIID_MEDIACDR)</summary>
    MediaCdR = 67,
    /// <summary>A writable CD in the process of being burned (SIID_MEDIACDBURN)</summary>
    MediaCdBurn = 68,
    /// <summary>Blank writable CD media (SIID_MEDIABLANKCD)</summary>
    MediaBlankCd = 69,
    /// <summary>CD-ROM media (SIID_MEDIACDROM)</summary>
    MediaCdRom = 70,
    /// <summary>An audio file (SIID_AUDIOFILES)</summary>
    AudioFiles = 71,
    /// <summary>An image file (SIID_IMAGEFILES)</summary>
    ImageFiles = 72,
    /// <summary>A video file (SIID_VIDEOFILES)</summary>
    VideoFiles = 73,
    /// <summary>A mixed file (SIID_MIXEDFILES)</summary>
    MixedFiles = 74,
    /// <summary>Folder back (SIID_FOLDERBACK)</summary>
    FolderBack = 75,
    /// <summary>Folder front (SIID_FOLDERFRONT)</summary>
    FolderFront = 76,
    /// <summary>Security shield, use for UI that requires administrator rights (SIID_SHIELD)</summary>
    Shield = 77,
    /// <summary>Warning (SIID_WARNING)</summary>
    Warning = 78,
    /// <summary>Informational (SIID_INFO)</summary>
    Info = 79,
    /// <summary>Error (SIID_ERROR)</summary>
    Error = 80,
    /// <summary>Key (SIID_KEY)</summary>
    Key = 81,
    /// <summary>Software (SIID_SOFTWARE)</summary>
    Software = 82,
    /// <summary>A UI item, such as a button, that issues a rename command (SIID_RENAME)</summary>
    Rename = 83,
    /// <summary>A UI item, such as a button, that issues a delete command (SIID_DELETE)</summary>
    Delete = 84,
    /// <summary>Audio DVD media (SIID_MEDIAAUDIODVD)</summary>
    MediaAudioDvd = 85,
    /// <summary>Movie DVD media (SIID_MEDIAMOVIEDVD)</summary>
    MediaMovieDvd = 86,
    /// <summary>Enhanced CD media (SIID_MEDIAENHANCEDCD)</summary>
    MediaEnhancedCd = 87,
    /// <summary>Enhanced DVD media (SIID_MEDIAENHANCEDDVD)</summary>
    MediaEnhancedDvd = 88,
    /// <summary>High definition DVD media in the HD DVD format (SIID_MEDIAHDDVD)</summary>
    MediaHdDvd = 89,
    /// <summary>High definition DVD media in the Blu-ray Disc format (SIID_MEDIABLURAY)</summary>
    MediaBluRay = 90,
    /// <summary>Video CD (VCD) media (SIID_MEDIAVCD)</summary>
    MediaVcd = 91,
    /// <summary>DVD+R media (SIID_MEDIADVDPLUSR)</summary>
    MediaDvdPlusR = 92,
    /// <summary>DVD+RW media (SIID_MEDIADVDPLUSRW)</summary>
    MediaDvdPlusRw = 93,
    /// <summary>A desktop computer (SIID_DESKTOPPC)</summary>
    DesktopPc = 94,
    /// <summary>A mobile computer, laptop (SIID_MOBILEPC)</summary>
    MobilePc = 95,
    /// <summary>The User Accounts Control Panel item (SIID_USERS)</summary>
    Users = 96,
    /// <summary>Smart media (SIID_MEDIASMARTMEDIA)</summary>
    MediaSmartMedia = 97,
    /// <summary>CompactFlash media (SIID_MEDIACOMPACTFLASH)</summary>
    MediaCompactFlash = 98,
    /// <summary>A cell phone (SIID_DEVICECELLPHONE)</summary>
    DeviceCellPhone = 99,
    /// <summary>A digital camera (SIID_DEVICECAMERA)</summary>
    DeviceCamera = 100,
    /// <summary>A digital video camera (SIID_DEVICEVIDEOCAMERA)</summary>
    DeviceVideoCamera = 101,
    /// <summary>An audio player (SIID_DEVICEAUDIOPLAYER)</summary>
    DeviceAudioPlayer = 102,
    /// <summary>Connect to network (SIID_NETWORKCONNECT)</summary>
    NetworkConnect = 103,
    /// <summary>The Network and Internet Control Panel item (SIID_INTERNET)</summary>
    Internet = 104,
    /// <summary>A compressed file with a .zip file name extension (SIID_ZIPFILE)</summary>
    ZipFile = 105,
    /// <summary>The Additional Options Control Panel item (SIID_SETTINGS)</summary>
    Settings = 106,
    /// <summary>HD DVD drive (SIID_DRIVEHDDVD), Windows Vista SP1 and later</summary>
    DriveHdDvd = 132,
    /// <summary>Blu-ray Disc (BD) drive (SIID_DRIVEBD), Windows Vista SP1 and later</summary>
    DriveBd = 133,
    /// <summary>High definition DVD-ROM media in the HD DVD-ROM format (SIID_MEDIAHDDVDROM), Windows Vista SP1 and later</summary>
    MediaHdDvdRom = 134,
    /// <summary>High definition DVD-R media in the HD DVD-R format (SIID_MEDIAHDDVDR), Windows Vista SP1 and later</summary>
    MediaHdDvdR = 135,
    /// <summary>High definition DVD-RAM media in the HD DVD-RAM format (SIID_MEDIAHDDVDRAM), Windows Vista SP1 and later</summary>
    MediaHdDvdRam = 136,
    /// <summary>Blu-ray Disc (BD) read-only media (SIID_MEDIABDROM), Windows Vista SP1 and later</summary>
    MediaBdRom = 137,
    /// <summary>Blu-ray Disc (BD) write-once media (SIID_MEDIABDR), Windows Vista SP1 and later</summary>
    MediaBdR = 138,
    /// <summary>Blu-ray Disc (BD) rewritable media (SIID_MEDIABDRE), Windows Vista SP1 and later</summary>
    MediaBdRe = 139,
    /// <summary>A cluster disk array (SIID_CLUSTEREDDRIVE), Windows Vista SP1 and later</summary>
    ClusteredDrive = 140,
    /// <summary>The highest valid value in the enumeration (SIID_MAX_ICONS), not an icon. Values over 160 are Windows 7 only icons</summary>
    MaxIcons = 181,
}
