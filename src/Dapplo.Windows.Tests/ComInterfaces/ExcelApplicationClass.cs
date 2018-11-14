using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Tests.ComInterfaces
{
    [ClassInterface((short)0)]
    [ComSourceInterfaces("Microsoft.Office.Interop.Excel.AppEvents")]
    [Guid("00024500-0000-0000-C000-000000000046")]
    [TypeLibType(2)]
    [ComImport]
    public class ExcelApplicationClass : IExcelApplication
    {
    }
}
