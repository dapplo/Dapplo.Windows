//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Dapplo.Windows.Kernel32.Enums
{
    /// <summary>
    ///     Windows Product types, returned by GetProductInfo
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowsProducts : uint
    {
#pragma warning disable 1591
        PRODUCT_UNDEFINED = 0x00000000,
        [Description("Ultimate")] PRODUCT_ULTIMATE = 0x00000001,
        [Description("Home Basic")] PRODUCT_HOME_BASIC = 0x00000002,
        [Description("Home Premium")] PRODUCT_HOME_PREMIUM = 0x00000003,
        [Description("Enterprise")] PRODUCT_ENTERPRISE = 0x00000004,
        [Description("Home Basic N")] PRODUCT_HOME_BASIC_N = 0x00000005,
        [Description("Business")] PRODUCT_BUSINESS = 0x00000006,
        [Description("Server Standard")] PRODUCT_STANDARD_SERVER = 0x00000007,
        [Description("Datacenter Server")] PRODUCT_DATACENTER_SERVER = 0x00000008,
        [Description("Smallbusines Server")] PRODUCT_SMALLBUSINESS_SERVER = 0x00000009,
        [Description("Server Enterprise (full installation)")] PRODUCT_ENTERPRISE_SERVER = 0x0000000A,
        [Description("Starter")] PRODUCT_STARTER = 0x0000000B,
        [Description("Server Datacenter (core installation)")] PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C,
        [Description("Server Standard (core installation)")] PRODUCT_STANDARD_SERVER_CORE = 0x0000000D,
        [Description("Server Enterprise (core installation)")] PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E,
        [Description("Server Enterprise for Itanium-based Systems")] PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F,
        [Description("Business N")] PRODUCT_BUSINESS_N = 0x00000010,
        [Description("Web Server (full installation)")] PRODUCT_WEB_SERVER = 0x00000011,
        [Description("HPC Edition")] PRODUCT_CLUSTER_SERVER = 0x00000012,
        [Description("Windows Storage Server")] PRODUCT_HOME_SERVER = 0x00000013,
        [Description("Storage Server Express")] PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014,
        [Description("Storage Server Standard")] PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015,
        [Description("Storage Server Workgroup")] PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016,
        [Description("Enterprise Storage Server")] PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017,
        [Description("Windows Server for Windows Essential Server Solutions")] PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018,
        [Description("Small Business Server Premium")] PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019,
        [Description("Home Premium N")] PRODUCT_HOME_PREMIUM_N = 0x0000001A,
        [Description("Enterprise N")] PRODUCT_ENTERPRISE_N = 0x0000001B,
        [Description("Ultimate N")] PRODUCT_ULTIMATE_N = 0x0000001C,
        [Description("Web Server (core installation)")] PRODUCT_WEB_SERVER_CORE = 0x0000001D,
        [Description("Windows Essential Business Server Management Server")] PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E,
        [Description("Windows Essential Business Server Security Server")] PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F,
        [Description("Windows Essential Business Server Messaging Server")] PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020,
        [Description("Server Foundation")] PRODUCT_SERVER_FOUNDATION = 0x00000021,
        [Description("Windows Home Server")] PRODUCT_HOME_PREMIUM_SERVER = 0x00000022,
        [Description("Windows Server without Hyper-V for Windows Essential Server Solutions")] PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023,
        [Description("Server Standard without Hyper-V")] PRODUCT_STANDARD_SERVER_V = 0x00000024,
        [Description("Server Datacenter without Hyper-V (full installation)")] PRODUCT_DATACENTER_SERVER_V = 0x00000025,
        [Description("Server Enterprise without Hyper-V (full installation)")] PRODUCT_ENTERPRISE_SERVER_V = 0x00000026,
        [Description("Server Datacenter without Hyper-V (core installation)")] PRODUCT_DATACENTER_SERVER_CORE_V = 0x00000027,
        [Description("Server Standard without Hyper-V (core installation)")] PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028,
        [Description("Server Enterprise without Hyper-V (core installation)")] PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029,
        [Description("Microsoft Hyper-V Server")] PRODUCT_HYPERV = 0x0000002A,
        [Description("Storage Server Express (core installation)")] PRODUCT_STORAGE_EXPRESS_SERVER_CORE = 0x0000002B,
        [Description("Storage Server Standard (core installation)")] PRODUCT_STORAGE_STANDARD_SERVER_CORE = 0x0000002C,
        [Description("Storage Server Workgroup (core installation)")] PRODUCT_STORAGE_WORKGROUP_SERVER_CORE = 0x0000002D,
        [Description("Storage Server Enterprise (core installation)")] PRODUCT_STORAGE_ENTERPRISE_SERVER_CORE = 0x0000002E,
        [Description("Starter N")] PRODUCT_STARTER_N = 0x0000002F,
        [Description("Professional")] PRODUCT_PROFESSIONAL = 0x00000030,
        [Description("Professional N")] PRODUCT_PROFESSIONAL_N = 0x00000031,
        [Description("Windows Small Business Server")] PRODUCT_SB_SOLUTION_SERVER = 0x00000032,
        [Description("Server For SB Solutions")] PRODUCT_SERVER_FOR_SB_SOLUTIONS = 0x00000033,
        [Description("Server Solutions Premium")] PRODUCT_STANDARD_SERVER_SOLUTIONS = 0x00000034,
        [Description("Server Solutions Premium (core installation)")] PRODUCT_STANDARD_SERVER_SOLUTIONS_CORE = 0x00000035,
        [Description("Server For SB Solutions EM")] PRODUCT_SB_SOLUTION_SERVER_EM = 0x00000036,
        [Description("Server For SB Solutions EM")] PRODUCT_SERVER_FOR_SB_SOLUTIONS_EM = 0x00000037,
        [Description("Windows MultiPoint Server")] PRODUCT_SOLUTION_EMBEDDEDSERVER = 0x00000038,
        [Description("Windows MultiPoint Server (core installation)")] PRODUCT_SOLUTION_EMBEDDEDSERVER_CORE = 0x00000039,
        [Description("Professional Embedded")] PRODUCT_PROFESSIONAL_EMBEDDED = 0x0000003A,
        [Description("Windows Essential Server Solution Management")] PRODUCT_ESSENTIALBUSINESS_SERVER_MGMT = 0x0000003B,
        [Description("Windows Essential Server Solution Additional")] PRODUCT_ESSENTIALBUSINESS_SERVER_ADDL = 0x0000003C,
        [Description("Windows Essential Server Solution Management SVC")] PRODUCT_ESSENTIALBUSINESS_SERVER_MGMTSVC = 0x0000003D,
        [Description("Windows Essential Server Solution Additional SVC")] PRODUCT_ESSENTIALBUSINESS_SERVER_ADDLSVC = 0x0000003E,
        [Description("Small Business Server Premium (core installation)")] PRODUCT_SMALLBUSINESS_SERVER_PREMIUM_CORE = 0x0000003F,
        [Description("Server Hyper Core V")] PRODUCT_CLUSTER_SERVER_V = 0x00000040,
        [Description("Embedded")] PRODUCT_EMBEDDED = 0x00000041,
        [Description("Starter E")] PRODUCT_STARTER_E = 0x00000042,
        [Description("Home Basic E")] PRODUCT_HOME_BASIC_E = 0x00000043,
        [Description("Home Premium E")] PRODUCT_HOME_PREMIUM_E = 0x00000044,
        [Description("Professional E")] PRODUCT_PROFESSIONAL_E = 0x00000045,
        [Description("Enterprise E")] PRODUCT_ENTERPRISE_E = 0x00000046,
        [Description("Ultimate E")] PRODUCT_ULTIMATE_E = 0x00000047,
        [Description("Enterprice Evaluation")] PRODUCT_ENTERPRISE_EVALUATION = 0x00000048,
        [Description("Windows MultiPoint Server Standard (full installation)")] PRODUCT_MULTIPOINT_STANDARD_SERVER = 0x0000004C,
        [Description("Windows MultiPoint Server Premium (full installation)")] PRODUCT_MULTIPOINT_PREMIUM_SERVER = 0x0000004D,
        [Description("Server Standard (evaluation installation)")] PRODUCT_STANDARD_EVALUATION_SERVER = 0x0000004F,
        [Description("Server Datacenter (evaluation installation)")] PRODUCT_DATACENTER_EVALUATION_SERVER = 0x00000050,
        [Description("Enterprise N Evaluation")] PRODUCT_ENTERPRISE_N_EVALUATION = 0x00000054,
        [Description("Embedded Automotive")] PRODUCT_EMBEDDED_AUTOMOTIVE = 0x00000055,
        [Description("Embedded Industry A")] PRODUCT_EMBEDDED_INDUSTRY_A = 0x00000056,
        [Description("Thin PC")] PRODUCT_THINPC = 0x00000057,
        [Description("Embedded A")] PRODUCT_EMBEDDED_A = 0x00000058,
        [Description("Embedded Industry")] PRODUCT_EMBEDDED_INDUSTRY = 0x00000059,
        [Description("Embedded E")] PRODUCT_EMBEDDED_E = 0x0000005A,
        [Description("Embedded Industry E")] PRODUCT_EMBEDDED_INDUSTRY_E = 0x0000005B,
        [Description("Embedded Industry A E")] PRODUCT_EMBEDDED_INDUSTRY_A_E = 0x0000005C,
        [Description("Storage Server Workgroup (evaluation installation)")] PRODUCT_STORAGE_WORKGROUP_EVALUATION_SERVER = 0x0000005F,
        [Description("Storage Server Standard (evaluation installation)")] PRODUCT_STORAGE_STANDARD_EVALUATION_SERVER = 0x00000060,
        [Description("Core ARM")] PRODUCT_CORE_ARM = 0x00000061,
        [Description("Core N")] PRODUCT_CORE_N = 0x00000062,
        [Description("Home China")] PRODUCT_CORE_COUNTRYSPECIFIC = 0x00000063,
        [Description("Home Single Language")] PRODUCT_CORE_SINGLELANGUAGE = 0x00000064,
        [Description("Home")] PRODUCT_CORE = 0x00000065,
        [Description("Professional with Media Center")] PRODUCT_PROFESSIONAL_WMC = 0x00000067,
        [Description("Mobile")] PRODUCT_MOBILE_CORE = 0x00000068,
        [Description("Embedded Industry (evaluation installation)")] PRODUCT_EMBEDDED_INDUSTRY_EVAL = 0x00000069,
        [Description("Embedded Industry E (evaluation installation)")] PRODUCT_EMBEDDED_INDUSTRY_E_EVAL = 0x0000006A,
        [Description("Embedded (evaluation installation)")] PRODUCT_EMBEDDED_EVAL = 0x0000006B,
        [Description("Embedded E (evaluation installation)")] PRODUCT_EMBEDDED_E_EVAL = 0x0000006C,
        [Description("Nano Server")] PRODUCT_NANO_SERVER = 0x0000006D,
        [Description("Cloud storage server")] PRODUCT_CLOUD_STORAGE_SERVER = 0x0000006E,
        [Description("Core Connected")] PRODUCT_CORE_CONNECTED = 0x0000006F,
        [Description("Professional Student")] PRODUCT_PROFESSIONAL_STUDENT = 0x00000070,
        [Description("Core Connected N")] PRODUCT_CORE_CONNECTED_N = 0x00000071,
        [Description("Professional Student N")] PRODUCT_PROFESSIONAL_STUDENT_N = 0x00000072,
        [Description("Core Connected Single Language")] PRODUCT_CORE_CONNECTED_SINGLELANGUAGE = 0x00000073,
        [Description("Core Connected China")] PRODUCT_CORE_CONNECTED_COUNTRYSPECIFIC = 0x00000074,
        [Description("Connected Car")] PRODUCT_CONNECTED_CAR = 0x00000075,
        [Description("Industry Handeld")] PRODUCT_INDUSTRY_HANDHELD = 0x00000076,
        [Description("PPI Professional")] PRODUCT_PPI_PRO = 0x00000077,
        [Description("ARM64 Sever")] PRODUCT_ARM64_SERVER = 0x00000078,
        [Description("Education")] PRODUCT_EDUCATION = 0x00000079,
        [Description("Education N")] PRODUCT_EDUCATION_N = 0x0000007A,
        [Description("IoT Core")] PRODUCT_IOTUAP = 0x0000007B,
        [Description("Cloud Host infrastructure Server")] PRODUCT_CLOUD_HOST_INFRASTRUCTURE_SERVER = 0x0000007C,
        [Description("Enterprise LTSB")] PRODUCT_ENTERPRISE_S = 0x0000007D,
        [Description("Windows 10 Enterprise 2015 LTSB N")] PRODUCT_ENTERPRISE_S_N = 0x0000007E,
        [Description("Professional S")] PRODUCT_PROFESSIONAL_S = 0x0000007F,
        [Description("Professional LTSB N")] PRODUCT_PROFESSIONAL_S_N = 0x00000080,
        [Description("Windows 10 Enterprise 2015 LTSB Evaluation")] PRODUCT_ENTERPRISE_S_EVALUATION = 0x00000081,
        [Description("Windows 10 Enterprise 2015 LTSB N Evaluation")] PRODUCT_ENTERPRISE_S_N_EVALUATION = 0x00000082,
        [Description("IoT Core Commercial")] PRODUCT_IOTUAPCOMMERCIAL = 0x00000083,
        [Description("Unlicensed")] PRODUCT_UNLICENSED = 0xABCDABCD
#pragma warning restore 1591
    }
}