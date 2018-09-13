using System;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace Boo.Lang.CodeDom
{
    [Serializable]
    public class FakeProcessMethodBodies : ProcessMethodBodiesWithDuckTyping
    {
        public override void OnMethod(Method node)
        {
            if (node.Name.Equals("InitializeComponent"))
            {
                base.OnMethod(node);
            }
            else
            {
                this.MarkVisited(node);
                this.Visit(node.Attributes);
                this.Visit(node.Parameters);
                this.Visit(node.ReturnType);
                this.Visit(node.ReturnTypeAttributes);
                this.GetEntity(node);
            }
        }
    }
}
