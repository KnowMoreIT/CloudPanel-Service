using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CloudPanel Service")]
[assembly: AssemblyDescription("Service that runs scheduled tasks for CloudPanel")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Know More IT")]
[assembly: AssemblyProduct("CloudPanel Service")]
[assembly: AssemblyCopyright("Copyright ©  2016 Know More IT")]
[assembly: AssemblyTrademark("CloudPanel")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8f472643-25e8-4162-97a6-6983e1600dba")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Log4net
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
