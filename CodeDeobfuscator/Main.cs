using DeobfuscateMain;
using Mono.Cecil;

namespace CodeDeobfuscator
{
	public class Main : Patcher
	{
		public override string Name { get { return "CodeDeobfuscator"; } }
		public override string[] Authors { get { return new[] {"DerPopo"}; } }

		public override void Patch(Logger logger, AssemblyDefinition asmCSharp, AssemblyDefinition __reserved)
		{
			DecryptStrings.Apply(asmCSharp.Modules[0], logger);
			GarbageRemover.Apply(asmCSharp.Modules[0], logger);
		}
	}
}

