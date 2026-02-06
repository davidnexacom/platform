using FSH.Framework.Web.Modules;
using System.Runtime.CompilerServices;

[assembly: FshModule(typeof(FSH.Modules.Messaging.MessagingModule), 400)]
[assembly: InternalsVisibleTo("Modules.Messaging.Tests")]
