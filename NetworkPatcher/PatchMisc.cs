using System;
using DeobfuscateMain;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Text;

namespace NetworkPatcher
{
	public class PatchMisc
	{
		public static void Patch(Logger logger, AssemblyDefinition asmCSharp)
		{
			ModuleDefinition module = asmCSharp.Modules[0];

			//-----------------------------TileEntities-----------------------------
			TileEntityPatcher.Patch(logger, asmCSharp);

			//--------------------------AuthenticatePlayer--------------------------
			HelperClass.executeActions(module, "GameManager", new []{
				HelperClass.MethodParametersComparer("ClientInfo", "GameUtils/KickPlayerData"),
				HelperClass.MethodReturnTypeComparer("System.Void"),
			}, HelperClass.MemberNameSetter<MethodDefinition>("DenyPlayer"));
		}
	}
}

