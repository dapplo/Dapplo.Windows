using System.Runtime.InteropServices;

namespace Dapplo.Windows.Tests.ComInterfaces;

[Guid("000208D5-0000-0000-C000-000000000046")]
[CoClass(typeof(ExcelApplicationClass))]
[ComImport]
public interface IExcelApplication
{
}