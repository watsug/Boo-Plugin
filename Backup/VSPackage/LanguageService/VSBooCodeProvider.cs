﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boo.Lang.CodeDom;
using Hill30.Boo.ASTMapper;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Text;

namespace Hill30.BooProject.LanguageService
{
    class VSBooCodeProvider: BooCodeProvider
    {
        private readonly IFileNode _baseFileNode;

        public VSBooCodeProvider(string[] references, IFileNode baseFileNode) : base(references)
        {
            _baseFileNode = baseFileNode;
        }

        private string FileName { get; set; }

        public override CodeCompileUnit Parse(TextReader codeStream)
        {
            //
            string mainFilePath = GetFilePath();
            // Are we are from the Designer ?
            var ddtr = codeStream as DocDataTextReader;
            if (ddtr != null)
            {
                this.FileName = mainFilePath;
                // Do the parse
                // If the TextReader is a DocDataTextReader, we should be running from VisualStudio, called by the designer
                // So, we will guess the FileName to check if we have a .Designer.boo file at the same place.
                // If so, we will have to build both .boo files to produce the CodeCompileUnit
                // Now, we should check if we have a partial Class inside, if so, that's a Candidate for .Designer.boo
                // Ok, so get the Filename, to get the companion file
                var dd = ((IServiceProvider) ddtr).GetService(typeof(DocData)) as DocData;
                String ddFileName = dd.Name;
                // Build the Designer FileName
                var baseIsDesignForm = ddFileName.EndsWith(".Designer.boo");
                String companionFile = baseIsDesignForm
                    ? BooCodeDomHelper.BuildNonDesignerFileName(ddFileName)
                    : BooCodeDomHelper.BuildDesignerFileName(ddFileName);
                if (File.Exists(companionFile))
                {
                    // Ok, we have a candidate !!!
                    DocData docdata = new DocData(ddtr, companionFile);
                    DocDataTextReader reader = new DocDataTextReader(docdata);
                    // so parse
                    var result = base.Parse(new TextReader[]{codeStream, reader}, new []{ddFileName, companionFile});                    
                    BooCodeDomHelper.AnnotateCompileUnit(result);
                    return result;
                }
                
            }
            return base.Parse(codeStream);
        }

        // Called by the WinForms designer at save time
        public override void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer, CodeGeneratorOptions options)
        {
            /*
            // Does that CodeCompileUnit comes from a "Merged" unit ?
            if (compileUnit.UserData.Contains(BooCodeDomHelper.USERDATA_HASDESIGNER))
            {
                // Retrieve the Form Class
                CodeTypeDeclaration designerClass = BooCodeDomHelper.FindDesignerClass(compileUnit);
                // and retrieve the filename of the prg file
                String prgFileName = (string)compileUnit.UserData[BooCodeDomHelper.USERDATA_FILENAME];
                // Build the Designer FileName
                String designerPrgFile = BooCodeDomHelper.BuildDesignerFileName(prgFileName);
                //
                CodeTypeDeclaration formClass = BooCodeDomHelper.FindFirstClass(formCCU);
                CodeTypeDeclaration designClass = BooCodeDomHelper.FindFirstClass(designCCU);
                // Now, remove the members
                formClass.Members.Clear();
                designClass.Members.Clear();
                // Now, split the members
                foreach (CodeTypeMember ctm in designerClass.Members)
                {
                    // Was it a member that we have found in the original merged CodeCompileUnits ?
                    if (ctm.UserData.Contains(BooCodeDomHelper.USERDATA_FROMDESIGNER))
                    {
                        if ((bool)ctm.UserData[BooCodeDomHelper.USERDATA_FROMDESIGNER])
                        {
                            // Comes from the Designer.prg file
                            // so go back to Designer.prg
                            designClass.Members.Add(ctm);
                        }
                        else
                        {
                            // Comes from the original Form file
                            formClass.Members.Add(ctm);
                        }
                    }
                    else
                    {
                        // This must be a member generated by the Designer !
                        // So we will move Methods to the Form and all others to the Designer
                        if (ctm is CodeMemberMethod)
                        {
                            formClass.Members.Add(ctm);
                        }
                        else
                        {
                            designClass.Members.Add(ctm);
                        }
                    }
                }
                // now, we must save both CodeCompileUnit
                // The received TextWriter is pointing to the Form
                // so we must create our own TextWriter for the Designer
                // First, let's make in Memory
                String generatedSource;
                MemoryStream inMemory = new MemoryStream();
                StreamWriter designerStream = new StreamWriter(inMemory, Encoding.UTF8);
                // 
                base.GenerateCodeFromCompileUnit(designCCU, designerStream, options);
                // and force Flush
                designerStream.Flush();
                // Reset and read to String
                inMemory.Position = 0;
                StreamReader reader = new StreamReader(inMemory, Encoding.UTF8, true);
                generatedSource = reader.ReadToEnd();
                Encoding realencoding = reader.CurrentEncoding;
                reader.Close();
                designerStream.Close();
                // and now write the "real" file
                designerStream = new StreamWriter(designerPrgFile, false, realencoding);
                designerStream.Write(generatedSource);
                designerStream.Flush();
                designerStream.Close();
                NormalizeLineEndings(designerPrgFile);
                // The problem here, is that we "may" have some new members, like EvenHandlers, and we need to update their position (line/col)
                XSharpCodeParser parser = new XSharpCodeParser();
                parser.TabSize = XSharpCodeDomProvider.TabSize;
                parser.FileName = designerPrgFile;
                CodeCompileUnit resultDesigner = parser.Parse(generatedSource);
                CodeTypeDeclaration resultClass = BooCodeDomHelper.FindDesignerClass(resultDesigner);
                // just to be sure...
                if (resultClass != null)
                {
                    // Now push all elements from resultClass to designClass
                    designClass.Members.Clear();
                    foreach (CodeTypeMember ctm in resultClass.Members)
                    {
                        ctm.UserData[BooCodeDomHelper.USERDATA_FROMDESIGNER] = true;
                        designClass.Members.Add(ctm);
                    }
                }
                // Ok,we MUST do the same thing for the Form file
                base.GenerateCodeFromCompileUnit(formCCU, writer, options);
                // BUT, the writer is hold by the Form Designer, don't close  it !!
                writer.Flush();
                NormalizeLineEndings(prgFileName);
                // Now, we must re-read it and parse again
                IServiceProvider provider = (DocDataTextWriter)writer;
                DocData docData = (DocData)provider.GetService(typeof(DocData));
                DocDataTextReader ddtr = new DocDataTextReader(docData);
                // Retrieve 
                generatedSource = ddtr.ReadToEnd();
                // normalize the line endings
                generatedSource = generatedSource.Replace("\n", "");
                generatedSource = generatedSource.Replace("\r", "\r\n");
                // Don't forget to set the name of the file where the source is... 
                parser.FileName = prgFileName;
                resultDesigner = parser.Parse(generatedSource);
                resultClass = BooCodeDomHelper.FindFirstClass(resultDesigner);
                // just to be sure...
                if (resultClass != null)
                {
                    // Now push all elements from resultClass to formClass
                    formClass.Members.Clear();
                    foreach (CodeTypeMember ctm in resultClass.Members)
                    {
                        ctm.UserData[BooCodeDomHelper.USERDATA_FROMDESIGNER] = false;
                        formClass.Members.Add(ctm);
                    }
                }
                // Ok, it should be ok....
                // We have updated the file and the types that are stored inside each CCU that have been merged in compileUnit
                //BooCodeDomHelper.MergeCodeCompileUnit(compileUnit, formCCU, designCCU);
                // And update...
                designerClass.Members.Clear();
                foreach (CodeTypeMember m in designClass.Members)
                {
                    designerClass.Members.Add(m);
                }
                foreach (CodeTypeMember m in formClass.Members)
                {
                    designerClass.Members.Add(m);
                }
            }
            else
            {
                // suppress generating the "generated code" header
                compileUnit.UserData[BooCodeDomHelper.USERDATA_NOHEADER] = true;
                base.GenerateCodeFromCompileUnit(compileUnit, writer, options);
                writer.Flush();
                // Designer gave us these informations
                CodeTypeDeclaration formClass = BooCodeDomHelper.FindFirstClass(compileUnit);
                // Now, we must re-read it and parse again
                IServiceProvider provider = (DocDataTextWriter)writer;
                DocData docData = (DocData)provider.GetService(typeof(DocData));
                DocDataTextReader ddtr = new DocDataTextReader(docData);
                // Retrieve 
                string generatedSource = ddtr.ReadToEnd();
                XSharpCodeParser parser = new XSharpCodeParser();
                parser.TabSize = XSharpCodeDomProvider.TabSize;
                if (compileUnit.UserData.Contains(BooCodeDomHelper.USERDATA_FILENAME))
                {
                    parser.FileName = (string)compileUnit.UserData[BooCodeDomHelper.USERDATA_FILENAME];
                }
                CodeCompileUnit resultCcu = parser.Parse(generatedSource);
                CodeTypeDeclaration resultClass = BooCodeDomHelper.FindFirstClass(resultCcu);
                // just to be sure...
                if (resultClass != null)
                {
                    // Now push all elements from resultClass to formClass
                    formClass.Members.Clear();
                    foreach (CodeTypeMember ctm in resultClass.Members)
                    {
                        formClass.Members.Add(ctm);
                    }
                }

            }
            */
        }

        private string GetFilePath()
        {
            return _baseFileNode.Url;
        }
    }
}
