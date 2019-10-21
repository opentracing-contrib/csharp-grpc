using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Retail")]
#endif
[assembly: AssemblyCompany("Benjamin Krämer, Medialogia®")]
[assembly: AssemblyProduct("OpenTracing.Contrib.Grpc")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.2.0.0")]

[assembly: ComVisible(false)]

[assembly: NeutralResourcesLanguage("en")]