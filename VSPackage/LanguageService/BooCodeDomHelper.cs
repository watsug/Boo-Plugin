using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boo.Lang.Compiler.Ast;
using Microsoft.VisualStudio.Shell.Design.Serialization.CodeDom;

namespace Hill30.BooProject.LanguageService
{
    static class BooCodeDomHelper
    {
        public const string USERDATA_FROMDESIGNER = "Boo.FromDesigner";
        public const string USERDATA_HASDESIGNER = "Boo.HasDesigner";
        public const string USERDATA_FILENAME = "Boo.Filename";
        public const string USERDATA_CCU_FORM = "Boo.CCUForm";
        public const string USERDATA_CCU_DESIGNER = "Boo.CCUDesigner";
        public const string USERDATA_NOHEADER = "Boo.NoHeader";
        public const string DESIGNER_EXTENSION = ".Designer.boo";
        
        /// <summary>
        /// Reading the CodeCompileUnit, enumerate all NameSpaces, enumerate All Types, searching for the first Partial Class.
        /// </summary>
        /// <param name="ccu"></param>
        /// <param name="contextNameSpace">The NameSpace in wich the partial Class is defined</param>
        /// <param name="contextClass">The found partial Class</param>
        /// <returns>True if a partial Class has been found</returns>
        public static bool HasPartialClass(CodeCompileUnit ccu, out CodeNamespace contextNameSpace, out CodeTypeDeclaration contextClass)
        {
            var element = ccu.Namespaces.Cast<CodeNamespace>()
                .SelectMany(n => n.Types.Cast<CodeTypeDeclaration>(), (n, t) => new { ns = n, type = t})
                .FirstOrDefault(t => t.type.IsClass && t.type.IsPartial);
            if (element == null)
            {
                contextNameSpace = null;
                contextClass = null;
                return false;
            }
            contextNameSpace = element.ns;
            contextClass = element.type;
            return true;
        }

        /// <summary>
        /// Return the FileName with .Designer inserted
        /// </summary>
        /// <param name="booFile"></param>
        /// <returns></returns>
        public static string BuildDesignerFileName(string booFile)
        {
            // Retrieve path information from the FulPath
            String booPath = Path.GetDirectoryName(booFile);
            // Strip off the.prg
            String baseName = Path.GetFileNameWithoutExtension(booFile);
            // Does the FileName ends with .Designer ?
            if (!baseName.EndsWith(".Designer"))
                baseName += ".Designer";
            // Add the original file extension
            String ext = Path.GetExtension(booFile);
            //
            return Path.Combine(booPath, baseName) + ext;
        }

        public static string BuildNonDesignerFileName(string booFile)
        {
            if (!booFile.EndsWith(DESIGNER_EXTENSION))
                return booFile;
            var baseFilename = booFile.Substring(0, booFile.Length - DESIGNER_EXTENSION.Length);
            return baseFilename + ".boo";
        }

        internal static void AnnotateCompileUnit(CodeCompileUnit compileUnit)
        {
            var members = compileUnit.Namespaces.Cast<CodeNamespace>()
                .SelectMany(n => n.Types.Cast<CodeTypeDeclaration>())
                .SelectMany(t => t.Members.Cast<CodeTypeMember>());
            foreach (var member in members)
                SetDesignerData(member);
        }

        private static CodeTypeDeclaration FindMainClass(CodeCompileUnit compileUnit, CodeNamespace designerNamespace, string designerClassName)
        {
            return compileUnit.Namespaces
                .Cast<CodeNamespace>()
                .Where(ns => ns.Name.Equals(designerNamespace.Name))
                .SelectMany(ns => ns.Types.Cast<CodeTypeDeclaration>())
                .SingleOrDefault(td => td.Name.Equals(designerClassName) && !IsDesignerClass(td));
        }

        /// <summary>
        /// Merge both CodeCompileUnit. The main type (class) will come from designerCompileUnit
        /// </summary>
        /// <param name="compileUnit"></param>
        /// <param name="designerCompileUnit"></param>
        /// <returns></returns>
        internal static CodeCompileUnit MergeCodeCompileUnit(CodeCompileUnit compileUnit, bool baseIsDesignForm)
        {
            // Create the merged CodeCompileUnit
            var mergedCodeCompileUnit = new CodeCompileUnit();
            //
            CodeNamespace designerNamespace;
            CodeTypeDeclaration designerClass = FindDesignerClass(compileUnit, out designerNamespace);
            if (designerClass != null)
            {
                // Do the same with the form
                var mainClass = FindMainClass(compileUnit, designerNamespace, designerClass.Name);
                // and merge only if ...
                if (mainClass != null)
                {
                    // Ok, same Namespace & same Class : Merge !

                    // So, the "main" class is...
                    CodeTypeDeclaration mergedType = new CodeTypeDeclaration(mainClass.Name);
                    // And does inherit from
                    mergedType.BaseTypes.AddRange(mainClass.BaseTypes);
                    mergedType.TypeAttributes = mainClass.TypeAttributes;
                    // Now, read members from each side, and put a stamp on each
                    foreach (CodeTypeMember member in designerClass.Members)
                    {
                        member.UserData[USERDATA_FROMDESIGNER] = true;
                        mergedType.Members.Add(member);
                        SetDesignerData(member);
                    }
                    foreach (CodeTypeMember member in mainClass.Members)
                    {
                        member.UserData[USERDATA_FROMDESIGNER] = false;
                        mergedType.Members.Add(member);
                        SetDesignerData(member);
                    }
                    // A class is always in a NameSpace
                    CodeNamespace mergedNamespace = new CodeNamespace(designerNamespace.Name);
                    mergedNamespace.Types.Add(mergedType);
                    // Now, add it to the CompileUnit
                    mergedCodeCompileUnit.Namespaces.Clear();
                    mergedCodeCompileUnit.Namespaces.Add(mergedNamespace);
                    //
                }
                else
                {
                    // Something went wrong, return the designer CodeCompileUnit
                    mergedCodeCompileUnit = compileUnit;
                }
            }
            else
            {
                // Sorry, no designer class
                mergedCodeCompileUnit = compileUnit;
            }
            return mergedCodeCompileUnit;
        }

        private static void SetDesignerData(CodeTypeMember member)
        {
            var li = member.UserData["LexicalInfo"] as LexicalInfo;
            if (li == null)
                return;

            var designerData = new CodeDomDesignerData
            {
                CaretPosition = new System.Drawing.Point(li.Column, li.Line),
                FileName = li.FileName
            };
            member.UserData[typeof(CodeDomDesignerData)] = designerData;
            member.UserData[typeof(System.Drawing.Point)] = designerData.CaretPosition;
        }

        /// <summary>
        /// Reading the CodeCompileUnit, enumerate all NameSpaces, enumerate All Types, searching for the first Class that contains an InitializeComponent member
        /// </summary>
        /// <param name="ccu"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        internal static CodeTypeDeclaration FindDesignerClass(CodeCompileUnit ccu)
        {
            CodeNamespace namespaceName;
            return FindDesignerClass(ccu, out namespaceName);
        }

        internal static bool IsDesignerClass(CodeTypeDeclaration cls)
        {
            if (cls.IsClass)
            {
                // Looking for InitializeComponent, returning a void, and with no Parameters
                foreach (CodeTypeMember member in cls.Members)
                {
                    CodeMemberMethod method = member as CodeMemberMethod;
                    if ((method != null) &&
                        (method.Name == "InitializeComponent") &&
                        (method.ReturnType.BaseType == "System.Void") &&
                        (method.ReturnType.TypeArguments.Count == 0) &&
                        (method.Parameters.Count == 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static CodeTypeDeclaration FindDesignerClass(CodeCompileUnit ccu, out CodeNamespace namespaceName)
        {
            namespaceName = null;
            // We search the first Class that has a Candidate for InitializeComponent
            foreach (CodeNamespace nameSpace in ccu.Namespaces)
            {
                foreach (CodeTypeDeclaration typeElement in nameSpace.Types)
                {
                    if (IsDesignerClass(typeElement))
                    {
                        // This one seems to be ok
                        // Return where it is
                        namespaceName = nameSpace;
                        // and what it is
                        return typeElement;
                    }
                }
            }
            // No way
            return null;
        }

    }
}
