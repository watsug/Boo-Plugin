using Boo.Lang.Compiler.Pipelines;

namespace boocNET451
{
    public class PipeLine : CompileToFile
    {
        public PipeLine()
        {
            BreakOnErrors = false;
        }
    }
}
