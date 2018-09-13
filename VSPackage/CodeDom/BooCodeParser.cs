using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Environments;
using Boo.Lang.Runtime;
using CompilerGenerated;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Boo.Lang.CodeDom
{

    [Serializable]
    public class BooCodeParser : ICodeParser
    {
        [Serializable]
        internal class DoParseLocals53
        {
            internal CompilerContext context;
            internal BooCodeDomConverter converter;
        }

        [Serializable]
        internal class DoParseClosure6
        {
            internal DoParseLocals53 _locals54;

            public DoParseClosure6(DoParseLocals53 locals54)
            {

                this._locals54 = locals54;
            }

            public void Invoke()
            {
                this._locals54.context.CompileUnit.Accept(this._locals54.converter);
            }
        }

        private string[] _references;

        public BooCodeParser(string[] references)
        {
            this._references = references;
        }

        private bool CompilerErrorFilter(Boo.Lang.Compiler.CompilerError e)
        {
            int num = e.Code.Equals("BCE0005") ? 1 : 0;
            if (num == 0)
            {
                num = (e.Code.Equals("BCE0018") ? 1 : 0);
            }
            if (num == 0)
            {
                num = (e.Code.Equals("BCE0019") ? 1 : 0);
            }
            if (num == 0)
            {
                num = (e.Code.Equals("BCE0064") ? 1 : 0);
            }
            return (byte)num != 0;
        }

        private CodeCompileUnit DoParse(params ICompilerInput[] inputs)
        {
            DoParseLocals53 DoParseLocals = new DoParseLocals53();
            BooCompiler booCompiler = new BooCompiler();
            booCompiler.Parameters.Pipeline = new ResolveExpressions();
            CompilerPipeline pipeline = booCompiler.Parameters.Pipeline;
            pipeline.BreakOnErrors = false;
            // TODO
            //pipeline.AfterStep += $adaptor$__BooCodeParser$callable2$27_25__$CompilerStepEventHandler$0.Adapt(this.$DoParse$closure$5);
            pipeline.Replace(typeof(ProcessMethodBodiesWithDuckTyping), new FakeProcessMethodBodies());
            booCompiler.Parameters.Input.AddRange(inputs);
            AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> hashSet = new HashSet<string>(this._references);
            hashSet.Add("System.Windows.Forms");
            hashSet.Add("System.Drawing");
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int i = 0;
            Assembly[] array = assemblies;
            for (int length = array.Length; i < length; i = checked(i + 1))
            {
                if (RuntimeServices.op_Member(array[i].GetName().Name, hashSet))
                {
                    hashSet.Remove(array[i].GetName().Name);
                    booCompiler.Parameters.References.Add(array[i]);
                }
            }
            DoParseLocals.context = booCompiler.Run();
            if (DoParseLocals.context.Errors.Count > 0)
            {
                throw ((Boo.Lang.List<Boo.Lang.Compiler.CompilerError>)DoParseLocals.context.Errors)[0];
            }
            DoParseLocals.converter = new BooCodeDomConverter();
            ActiveEnvironment.With(new InstantiatingEnvironment(), new DoParseClosure6(DoParseLocals).Invoke);
            return DoParseLocals.converter.CodeDomUnit;
        }

        public CodeCompileUnit Parse(TextReader codeStream)
        {
            string contents = codeStream.ReadToEnd();
            StringInput stringInput = new StringInput("input", contents);
            return this.DoParse(stringInput);
        }

        public CodeCompileUnit Parse(IEnumerable<TextReader> inputs, IEnumerable<string> names)
        {
            return this.DoParse(inputs.Zip(names, this.ParseClosure7).Cast<ICompilerInput>().ToArray());
        }

        internal Boo.Lang.List<Boo.Lang.Compiler.CompilerError> DoParseClosure5()
        {
            return My<CompilerContext>.Instance.Errors.RemoveAll(this.CompilerErrorFilter);
        }

        internal StringInput ParseClosure7(TextReader i, string n)
        {
            return new StringInput(n, i.ReadToEnd());
        }
    }
}
