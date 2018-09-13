using System;
using System.IO;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Boo.Lang.CodeDom
{
    class BooCodeGeneratorEx : BooCodeGenerator
    {
        private bool _specialIndent;

        public string GenerateNewCode(IEnumerable<CodeMemberMethod> methods, string indentString)
        {
            StringBuilder stringBuilder = new StringBuilder();
            CodeGeneratorOptions codeGeneratorOptions = new CodeGeneratorOptions();
            CodeGeneratorOptions codeGeneratorOptions2 = codeGeneratorOptions;
            bool flag2 = codeGeneratorOptions2.BlankLinesBetweenMembers = true;
            CodeGeneratorOptions codeGeneratorOptions3 = codeGeneratorOptions;
            bool flag4 = codeGeneratorOptions3.VerbatimOrder = true;
            CodeGeneratorOptions codeGeneratorOptions4 = codeGeneratorOptions;
            string text2 = codeGeneratorOptions4.IndentString = indentString;
            CodeGeneratorOptions options = codeGeneratorOptions;
            this._specialIndent = true;
            this._booIndent = 1;
            StringWriter writer;
            IDisposable disposable = (writer = new StringWriter(stringBuilder)) as IDisposable;
            try
            {
                IEnumerator<CodeMemberMethod> enumerator = methods.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        CodeMemberMethod current = enumerator.Current;
                        this.GenerateCodeFromMember(current, writer, options);
                    }
                }
                finally
                {
                    enumerator.Dispose();
                }
            }
            finally
            {
                if (disposable != null)
                {
                    disposable.Dispose();
                    disposable = null;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
