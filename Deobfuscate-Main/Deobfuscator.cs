using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Mono.Cecil;

namespace DeobfuscateMain
{
	public class Deobfuscator
	{
		private static string ownFolder;
		public static string sourceAssemblyPath;
		private static Logger mainLogger;

		private static void ErrorExit (string message, int returnCode = 1)
		{
			Console.WriteLine ();
			Logger.Level logLevel = (returnCode == 0) ? Logger.Level.KEYINFO : Logger.Level.ERROR;
			if (mainLogger != null)
			{
				if (message.Length > 0)
					mainLogger.Log(logLevel, message);
				mainLogger.Close ();
			}
			else
				Console.WriteLine(Logger.Level_ToString(logLevel) + message);

			Console.WriteLine ();
			Console.WriteLine ("Press any key to exit");
			Console.ReadKey ();
			Environment.Exit (returnCode);
		}

		public static void Main (string[] args)
		{
			Console.WriteLine ("Assembly-CSharp Deobfuscator for 7 Days to Die [by the 7 Days to Die Modding Community]");

			ownFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetEntryAssembly ().Location));
			if (ownFolder == null) {
				ErrorExit("Unable to retrieve the folder containing Deobfuscator!");
				return;
			}
			bool verbosity = false;//(args.Length > 1) ? (args[0].ToLower().Equals("-v")) : false;
			if (File.Exists (Path.Combine(ownFolder, "config.xml")))
			{
				XmlDocument configDoc = new XmlDocument ();
				try {
					configDoc.Load(Path.Combine(ownFolder, "config.xml"));
				} catch (Exception e) {
					Console.WriteLine(Logger.Level_ToString(Logger.Level.WARNING) + "Unable to load config.xml : " + e.ToString ());
				}
				XmlNodeList configElems = configDoc.DocumentElement.ChildNodes;
				foreach (XmlNode curElem in configElems) {
					if (!curElem.Name.ToLower().Equals("verbosity"))
						continue;
					XmlNode verbosityElem = curElem;
					XmlAttributeCollection verbosityAttrs = verbosityElem.Attributes;
					foreach (XmlNode curAttr in verbosityAttrs) {
						if (curAttr.Name.ToLower().Equals("enabled")) {
							verbosity = curAttr.Value.ToLower().Equals("true");
							break;
						}
					}
				}
			}
			else
				Console.WriteLine(Path.Combine(ownFolder, "config.xml"));
			mainLogger = new Logger (Path.Combine(ownFolder, "mainlog.txt"), null, (int)(verbosity ? Logger.Level.INFO : Logger.Level.KEYINFO));
			mainLogger.Info("Started logging to mainlog.txt.");

			if ( args.Length == 0 || !args[0].ToLower().EndsWith(".dll") )
			{
				mainLogger.Write("Usage : deobfuscate \"<path to file>\"");
				mainLogger.Write("Alternatively, you can drag and drop file into deobfuscate.");
				ErrorExit("", 2);
			}
			var acsharpSource = Path.GetFullPath(args [0]);
			if (!File.Exists (acsharpSource)) {
				ErrorExit("Unable to retrieve the folder containing " + args[0]);
				return;
			}
			sourceAssemblyPath = acsharpSource;

			string patchersPath = Path.Combine(ownFolder, "patchers");
			if (!Directory.Exists (patchersPath)) {
				Directory.CreateDirectory (patchersPath);
			}

			DefaultAssemblyResolver resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (Path.GetDirectoryName(acsharpSource));

			AssemblyDefinition csharpDef = null;
			AssemblyDefinition mscorlibDef = null;

			try {
				csharpDef = AssemblyDefinition.ReadAssembly (acsharpSource, new ReaderParameters{ AssemblyResolver = resolver });
			} catch (Exception e) {
				ErrorExit("Unable to load " + args[0] + " :" + e);
				return;
			}
			try {
				mscorlibDef = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(acsharpSource), "mscorlib.dll"), new ReaderParameters{ AssemblyResolver = resolver });
			} catch (Exception e) {
				mainLogger.Warning("Unable to load mscorlib.dll :" + e);
			}
			int csharpFileLen = (int)new FileInfo(acsharpSource).Length;
			if (csharpDef.Modules.Count == 0)
			{
				ErrorExit(args[0] + " is invalid!");
			}
			ModuleDefinition csharpModule = csharpDef.Modules[0];
			if (csharpModule.GetType("Deobfuscated") != null)
			{
				ErrorExit(args[0] + " already is deobfuscated!");
			}

			mainLogger.KeyInfo("Deobfuscating " + args[0] + "...");
			mainLogger.Write("___");
			string[] files = Directory.GetFiles(patchersPath, "*.dll");
			var patchers = new List<Patcher>();
			foreach (var file in files)
			{
				Assembly patcherAssembly;
				try
				{
					patcherAssembly = Assembly.LoadFrom(file);
				}
				catch (Exception e)
				{
					mainLogger.Error("Unable to load the patcher " + file + " :");
					mainLogger.Error(e.ToString());
					continue;
				}
				Type exttype = typeof (Patcher);
				Type extensiontype = null;
				foreach (var type in patcherAssembly.GetExportedTypes())
				{
					if (exttype.IsAssignableFrom(type))
					{
						extensiontype = type;
						break;
					}
				}
				if (extensiontype == null)
				{
					mainLogger.Error("Failed to load patcher " + file + " (Specified assembly does not implement an Patcher class)");
					continue;
				}

				// Create and register the extension
				try
				{
					var patcher = Activator.CreateInstance(extensiontype, new object[0]) as Patcher;
					if (patcher != null)
						patchers.Add(patcher);
				}
				catch (Exception e)
				{
					mainLogger.Error("Unable to instantiate the patcher class " + file + " :");
					mainLogger.Error(e.ToString());
				}
			}
			if (patchers.Count == 0)
			{
				ErrorExit("There are no patches to apply! Exiting.", 3);
				return;
			}
			foreach (var patcher in patchers)
			{
				mainLogger.KeyInfo("Executing patcher \"" + patcher.Name + "\" (by " + string.Join(",", patcher.Authors) + ")...");
				Logger curLogger = new Logger(Path.Combine(ownFolder, "log_" + patcher.Name + ".txt"), null, (int) (verbosity ? Logger.Level.INFO : Logger.Level.KEYINFO));
				try
				{
					patcher.Patch(curLogger, csharpDef, null);
				}
				catch (TargetInvocationException e)
				{
					mainLogger.Error("ERROR : Invoking the Patch method for " + patcher.Name + " resulted in an exception :");
					mainLogger.Error(e.StackTrace);
					mainLogger.Error(e.InnerException.StackTrace);
				}
				catch (Exception e)
				{
					mainLogger.Error("ERROR : An exception occured while trying to invoke the Patch method of " + patcher.Name + " :");
					mainLogger.Error(e.Message + Environment.NewLine + e.StackTrace);
				}
				curLogger.Close();
				mainLogger.Info("Writing the current Assembly-CSharp.dll to a MemoryStream...");
				var asmCSharpStream = new MemoryStream(csharpFileLen);
				csharpDef.Write(asmCSharpStream);
				mainLogger.Info("Reading the current Assembly-CSharp.dll from the MemoryStream...");
				asmCSharpStream.Seek(0, SeekOrigin.Begin);
				csharpDef = AssemblyDefinition.ReadAssembly(asmCSharpStream, new ReaderParameters {AssemblyResolver = resolver});
				asmCSharpStream.Close();
				csharpModule = csharpDef.Modules[0];
			}
			mainLogger.Write(); mainLogger.Write("___");

			if ((mscorlibDef != null) && (mscorlibDef.Modules.Count > 0))
			{
				csharpModule.Types.Add(new TypeDefinition("", "Deobfuscated", Mono.Cecil. TypeAttributes.Public, csharpDef.MainModule.TypeSystem.Object));
			}
			else
				mainLogger.Error("Unable to create the Deobufscated class!");
			string outputPath = Path.Combine(Path.GetDirectoryName(acsharpSource), Path.GetFileNameWithoutExtension(acsharpSource) + ".deobf.dll");
			mainLogger.KeyInfo ("Saving the new assembly to " + outputPath + " ...");
			try
			{
				csharpDef.Write (outputPath);
			}
			catch (Exception e)
			{
				ErrorExit ("Unable to save the assembly : " + e.Message + Environment.NewLine + e.StackTrace);
			}

			ErrorExit ("Success.", 0);
		}
	}
}
