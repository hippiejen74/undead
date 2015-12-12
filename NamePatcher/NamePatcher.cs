using DeobfuscateMain;
using Mono.Cecil;

namespace NamePatcher
{
	public class NamePatcher : Patcher
	{
		public override string Name { get { return "NameSimplifier"; } }
		public override string[] Authors { get { return new[] { "DerPopo" }; } }

		internal static ModuleDefinition module;
		public override void Patch(Logger logger, AssemblyDefinition asmCSharp, AssemblyDefinition __reserved)
		{
			module = asmCSharp.Modules[0];
			foreach (ModuleDefinition mdef in asmCSharp.Modules)
			{
				logger.KeyInfo("Patching " + mdef.Types.Count + " type[s] ...");
				foreach (TypeDefinition tdef in mdef.Types)
				{
					NameNormalizer.CheckNames(tdef);
				}
			}
			NameNormalizer.FinalizeNormalizing();
			NameNormalizer.clnamestomod.Clear();
			NameNormalizer.vclasses.Clear();
		}
	}
}
