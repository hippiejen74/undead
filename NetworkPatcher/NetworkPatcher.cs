using DeobfuscateMain;
using Mono.Cecil;

namespace NetworkPatcher
{
	public class NetworkPatcher : Patcher
	{
		public override string Name { get { return "PacketOrNotRelatedStuffPatcher"; } }
		public override string[] Authors { get { return new[] { "Alloc", "DerPopo", "KaXaK" }; } }

		public static int success = 0;
		public static int error = 0;

		public override void Patch(Logger logger, AssemblyDefinition asmCSharp, AssemblyDefinition __reserved)
		{
			/*var found = asmCSharp.MainModule.GetTypes().Any(type => type.Fields.Any(fi => fi.IsLiteral && !fi.IsInitOnly && fi.Constant.ToString().Contains("7DTD")));
			if (!found)
			{
				logger.Log(Logger.Level.KEYINFO, "Couldn't find 7DTD, skipping...");
				return;
			}*/
			PatchMisc.Patch(logger, asmCSharp);
		}
	}
}
