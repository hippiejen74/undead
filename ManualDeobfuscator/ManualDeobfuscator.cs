using System;
using System.Linq;
using Mono.Cecil;
using DeobfuscateMain;

namespace ManualDeobfuscator
{
	public class ManualDeobfuscator : Patcher
	{
		public override string Name { get { return "ManualDeobfuscator"; } }
		public override string[] Authors { get { return new[] { "Alloc", "DerPopo" }; } }

		public override void Patch(Logger logger, AssemblyDefinition asmCSharp, AssemblyDefinition __reserved)
		{
			/*var found = asmCSharp.MainModule.GetTypes().Any(type => type.Fields.Any(fi => fi.IsLiteral && !fi.IsInitOnly && fi.Constant.ToString().Contains("7DTD")));
			if (!found)
			{
				logger.Log(Logger.Level.KEYINFO, "Couldn't find 7DTD, skipping...");
				return;
			}*/
			PatchHelpers.logger = logger;
			ManualPatches.applyManualPatches(asmCSharp.MainModule);
			ManualPatches.FinalizeNormalizing();

			logger.Log(Logger.Level.KEYINFO, string.Format("Successful: {0} / Failed: {1}", PatchHelpers.success, PatchHelpers.errors));
		}
	}
}
