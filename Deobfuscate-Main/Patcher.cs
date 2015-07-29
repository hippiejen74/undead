using Mono.Cecil;

namespace DeobfuscateMain
{
	public abstract class Patcher
	{
		public abstract string Name { get; }

		public abstract string[] Authors { get; }

		public virtual void Patch(Logger logger, AssemblyDefinition asmCSharp, AssemblyDefinition __reserved)
		{
		}
	}
}
