using System.Runtime.CompilerServices;
using FSH.Framework.Web.Modules;

[assembly: FshModule(typeof(FSH.Modules.Identity.IdentityModule), 100)]
[assembly: InternalsVisibleTo("Identity.Tests")]
