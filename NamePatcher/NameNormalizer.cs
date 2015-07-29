﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace NamePatcher
{
    class NameNormalizer
    {
        private static readonly Regex FilterRegex = new Regex(@"^[a-zA-Z0-9\._<>\$`\-\[\],]*$", RegexOptions.Compiled);

        public static void CheckType(TypeDefinition tdef)
        {
            CheckNames(tdef);
        }

        public class vmdGroupInfo
        {
            public vmdGroupInfo(string newname, TypeDefinition baseclass)
            {
                this.newname = newname;
                this.baseclass = baseclass;
            }
            public List<MethodDefinition> applyingmdefs = new List<MethodDefinition>();
            public string newname;
            public TypeDefinition baseclass;
        };

        public static void setName(IMemberDefinition def, string name)
        {
            foreach (KeyValuePair<IMemberDefinition, string> entry in clnamestomod)
            {
                if (def.Equals(entry.Key))
                {
                    clnamestomod.Remove(def);
                    break;
                }
            }
            clnamestomod.Add(def, name);
        }
        public static string getName(IMemberDefinition def)
        {
            foreach (KeyValuePair<IMemberDefinition, string> entry in clnamestomod)
            {
                if (def.Equals(entry.Key))
                {
                    return entry.Value;
                }
            }
            return def.Name;
        }
        private static Boolean paramlistEquals(Mono.Collections.Generic.Collection<ParameterDefinition> _pars1,
                                                Mono.Collections.Generic.Collection<ParameterDefinition> _pars2)
        {
            if ((_pars1.Count != _pars2.Count) || _pars1.Count < 0)
                return false;
            ParameterDefinition[] pars1 = _pars1.ToArray();
            ParameterDefinition[] pars2 = _pars2.ToArray();
            for (int i = 0; i < _pars1.Count; i++)
            {
                ParameterDefinition par1 = pars1[i];
                ParameterDefinition par2 = pars2[i];
                if (par1 == null)
                {
                    if (par2 != null)
                        return false;
                }
                else if (par2 == null)
                {
                    if (par1 != null)
                        return false;
                }
                else if (!par1.ParameterType.Resolve().Equals(par2.ParameterType.Resolve()))
                    return false;
            }
            return true;
        }

        public static int vmethid = 1;
        public static int classid = 1;

        public static List<vmdGroupInfo> vclasses = new List<vmdGroupInfo>();
        public static Dictionary<IMemberDefinition, string> clnamestomod = new Dictionary<IMemberDefinition, string>();
        public static void CheckNames(TypeDefinition tdef)
        {
            //String newTName = makeValidName(getName(tdef));
            //if (newTName != null)
            //Console.WriteLine(getName(tdef) + " - " + nameIsObfuscated(getName(tdef)));
			if (nameIsObfuscated(getName(tdef)))
            {
                setName(tdef, "" + (tdef.IsClass ? "cl" : "tp") + String.Format("{0:x4}", classid)/*newTName*/);//tdef.Name = (tdef.IsClass ? "cl" : "tp") + newTName;
                classid++;
            }
            if (tdef.IsEnum)
                return;

            int cmid = 0;
            try
            {
                //if (tdef.HasInterfaces)
                //    cmid += checkTypeReferences(tdef.Interfaces, "if", cmid, tdef);
                if (tdef.HasNestedTypes)
                    cmid += checkLocalDefinitions<TypeDefinition>(tdef.NestedTypes, "scl", cmid, tdef);
            }
            catch (Exception e) { throw new Exception("occured while patching 1", e); }
            try
            {
                if (tdef.HasMethods)
                    checkLocalDefinitions<MethodDefinition>(tdef.Methods, "md", 0, tdef);
            }
            catch (Exception e) { throw new Exception("occured while patching 2", e); }
            try
            {
                if (tdef.HasFields)
                    checkLocalDefinitions<FieldDefinition>(tdef.Fields, "fd", 0, tdef);
            }
            catch (Exception e) { throw new Exception("occured while patching 3", e); }
            try
            {
                if (tdef.HasEvents)
                    checkLocalDefinitions<EventDefinition>(tdef.Events, "event", 0, tdef);
            }
            catch (Exception e) { throw new Exception("occured while patching 4", e); }
            try
            {
                if (tdef.HasProperties)
                    checkLocalDefinitions<PropertyDefinition>(tdef.Properties, "prop", 0, tdef);
            }
            catch (Exception e) { throw new Exception("occured while patching 5", e); }

        }
        static void checkLocalDefinition<T>(T def, string prefix, int cmid, TypeDefinition btdef) where T : IMemberDefinition
        {
			bool needsNewName = nameIsObfuscated(getName(def));
            //String newName = makeValidName(getName(def));
            if (typeof(T) == typeof(MethodDefinition))
            {
                MethodDefinition mdef = def as MethodDefinition;
				if (mdef.DeclaringType.GenericParameters.Count != 0 || mdef.GenericParameters.Count != 0)
					needsNewName = false;
				else
				{
	                Mono.Collections.Generic.Collection<ParameterDefinition> pardef = mdef.Parameters;
	                if (pardef == null) { pardef = new Mono.Collections.Generic.Collection<ParameterDefinition>(); }

	                int parid = 1;
					if (mdef.IsVirtual && needsNewName)
	                {
	                    prefix = "mdv";
	                    List<MethodDefinition> baseVmdefList = new List<MethodDefinition>();
	                    baseVmdefList.Add(mdef);
	                    TypeDefinition baseclass = null;
	                    try
	                    {
	                        TypeReference curBaseType = btdef.BaseType;
	                        while (curBaseType != null)
	                        {
	                            TypeDefinition basetdef = curBaseType.Resolve();
	                            if (basetdef == null)
	                                break;
	                            if (basetdef.HasMethods && basetdef.Methods != null)
	                            {
	                                foreach (MethodDefinition basemdef in basetdef.Methods)
	                                {
	                                    if (basemdef == null)
	                                        continue;
	                                    Mono.Collections.Generic.Collection<ParameterDefinition> basepardef = basemdef.Parameters;
	                                    if (basepardef == null) { basepardef = new Mono.Collections.Generic.Collection<ParameterDefinition>(); }
	                                    try
	                                    {
	                                        if (basemdef.Name != null && mdef.Name != null && basemdef.Name.Equals(mdef.Name) && basemdef.IsVirtual && paramlistEquals(basepardef, pardef))
	                                        {
	                                            baseVmdefList.Add(basemdef);
	                                            baseclass = basetdef;
	                                        }
	                                    }
	                                    catch (Exception) { /*throw new Exception("2.1");*/ }
	                                }
	                            }
	                            curBaseType = basetdef.BaseType;
	                        }

	                    }
	                    catch (NotSupportedException) { }
	                    if (baseclass != null)
	                    {
	                        vmdGroupInfo vmGroup = null;
	                        foreach (vmdGroupInfo curGroupInfo in vclasses)
	                        {
	                            if (curGroupInfo.applyingmdefs.Count < 1)
	                                continue;
	                            if (curGroupInfo.applyingmdefs.ToArray()[0].Name.Equals(mdef.Name)
	                                && curGroupInfo.baseclass.Name.Equals(baseclass.Name))
	                            {
	                                vmGroup = curGroupInfo;
	                                break;
	                            }
	                        }
	                        if (vmGroup == null)
	                        {
	                            vmGroup = new vmdGroupInfo(String.Format("{0}{1:x4}", prefix, vmethid), baseclass);
	                            vclasses.Add(vmGroup);
	                            ++vmethid;
	                        }
	                        int oldgrouplen = vmGroup.applyingmdefs.Count;
	                        object[] baseVmdefs = baseVmdefList.ToArray();
	                        for (int i = baseVmdefs.Length - 1; i >= 0; i--)
	                        {
	                            MethodDefinition curBaseDef = baseVmdefs[i] as MethodDefinition;
	                            foreach (MethodDefinition curSubDef in vmGroup.applyingmdefs)
	                            {
	                                if ((curBaseDef.DeclaringType == curSubDef.DeclaringType) && paramlistEquals(curSubDef.Parameters, mdef.Parameters))
	                                {
	                                    curBaseDef = null;
	                                    break;
	                                }
	                            }
	                            if (curBaseDef != null)
	                                vmGroup.applyingmdefs.Add(curBaseDef);
	                        }
							needsNewName = false;
	                    }
	                }
	                if (mdef.HasParameters)
	                {
	                    foreach (ParameterDefinition pdef in mdef.Parameters)
	                    {
							if (nameIsObfuscated(pdef.Name))
	                            pdef.Name = String.Format("par{0:x4}", parid);
	                        ++parid;
	                    }
	                }
				}
            }
			else if (typeof(T) == typeof(FieldDefinition))
			{
				FieldDefinition fdef = def as FieldDefinition;
				if (fdef.DeclaringType.GenericParameters.Count != 0)
					needsNewName = false;
			}
            //newName = (newName == null ? null : String.Format("{0}{1:x4}", prefix, cmid));
            //if (newName != null)
			if (needsNewName)
            {
				string newName = String.Format("{0}{1:x4}", prefix, cmid);
                setName(def, newName);
            }
            if (typeof(T) == typeof(TypeDefinition))
            {
                TypeDefinition tdef = def as TypeDefinition;
                if (!tdef.IsEnum)
                    CheckNames(tdef);
            }
            if (typeof(T) == typeof(PropertyDefinition))
            {
                PropertyDefinition pdef = def as PropertyDefinition;
                MethodDefinition getter = pdef.GetMethod;
                MethodDefinition setter = pdef.SetMethod;
            }
        }
        static int checkLocalDefinitions<T>(Mono.Collections.Generic.Collection<T> memDef, string prefix, int cmid, TypeDefinition btdef)
            where T : IMemberDefinition
        {
            if (memDef == null)
                return cmid;
            foreach (T def in memDef)
            {
                checkLocalDefinition<T>(def, prefix, cmid, btdef);
                ++cmid;
            }
            return cmid;
        }
        static int checkTypeReferences<T>(Mono.Collections.Generic.Collection<T> tRefs, string prefix, int cmid, TypeDefinition btdef)
            where T : MemberReference
        {
            if (tRefs == null)
                return cmid;
            foreach (MemberReference mref in tRefs)
            {
                try
                {
                    if (typeof(T) == typeof(TypeReference))
                    {
                        TypeDefinition tDef = (mref as TypeReference).Resolve();
                        checkLocalDefinition<TypeDefinition>(tDef, prefix, cmid, btdef);
                    }
                    else if (typeof(T) == typeof(MethodReference))
                    {
                        MethodDefinition mdDef = (mref as MethodReference).Resolve();
                        checkLocalDefinition<MethodDefinition>(mdDef, prefix, cmid, btdef);
                    }
                }
                catch (NotSupportedException) { }
            }
            return cmid;
        }
		static bool nameIsObfuscated(String origName)
		{
			if (string.IsNullOrEmpty(origName))
				return true;
			if (origName[0] >= '0' && origName[0] <= '9' || origName[0] == '=' || origName.Contains("\""))
				return true;
			bool hasOnlyUppercaseLetters = true;
			bool ret = (origName.Length == 5);
			foreach (char ch in origName)
			{
				if (
					(
						((ch & 0x00FF) > 0x7F) || (((ch & 0xFF00) >> 8) > 0x7F)
					) ||
					(("" + ch).Normalize().ToCharArray()[0] > 0x00FF) ||
					(((("" + ch).Normalize().ToCharArray()[0] & 0x00FF)) <= 0x20)
				)
				{
					return true;
				}
				if (!(ch >= 'A' && ch <= 'Z'))
				{
					hasOnlyUppercaseLetters = false;
				}
				if (!((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z')))
				{
					ret = false;
				}
			}
			if (!ret && !FilterRegex.IsMatch(origName))
				Console.WriteLine(origName);
			return ret || (hasOnlyUppercaseLetters && origName.Length < 5);
		}
        /*static String makeValidName(String origName)
        {
            if (origName == null)
                return "nullname";
            StringBuilder namebuilder = new StringBuilder(); bool modname = false;
            foreach (char ch in origName)
            {
                if (
                        (
                        ((ch & 0x00FF) > 0x7F) || (((ch & 0xFF00) >> 8) > 0x7F)
                        ) ||
                        (("" + ch).Normalize().ToCharArray()[0] > 0x00FF) ||
                        (((("" + ch).Normalize().ToCharArray()[0] & 0x00FF)) <= 0x20)
                    )
                {
                    Int16 test = (Int16)ch;
                    namebuilder.Append(String.Format("u{0:x4}", (ushort)ch));
                    modname = true;
                }
                else
                {
                    namebuilder.Append(ch);
                }
            }
            if (modname)
                return namebuilder.ToString();
            return null;
        }*/
        public static void FinalizeNormalizing()
        {

            foreach (KeyValuePair<IMemberDefinition, string> vce in NameNormalizer.clnamestomod)
            {
                try
                {
                    vce.Key.Name = vce.Value;
				}
                catch (Exception e) { Console.WriteLine("An exception occured : "); Console.WriteLine(e.ToString()); }
            }
            foreach (NameNormalizer.vmdGroupInfo curGroupEntry in NameNormalizer.vclasses)
            {
                try
                {
                    foreach (MethodDefinition curkey in curGroupEntry.applyingmdefs)
                    {
						curkey.Name = curGroupEntry.newname;
                    }
                }
                catch (Exception e) { Console.WriteLine("An exception occured : "); Console.WriteLine(e.ToString()); }
            }
        }
    }
}
