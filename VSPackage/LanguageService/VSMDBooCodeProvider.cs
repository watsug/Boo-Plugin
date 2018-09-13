using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.CodeDom;
using Hill30.Boo.ASTMapper;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Project;

namespace Hill30.BooProject.LanguageService
{
    class VSMDBooCodeProvider : IVSMDCodeDomProvider
    {
        private readonly BooCodeProviderEx _provider;
        private readonly ReferenceContainerNode _references;
        private readonly IFileNode _file;

        private IEnumerable<string> GetReferences(ReferenceContainerNode refs)
        {
            var node = refs.FirstChild;
            while (node != null)
            {
                yield return node.Caption;
                node = node.NextSibling;
            }
        }

        public VSMDBooCodeProvider(ReferenceContainerNode refs, IFileNode file)
        {
            _provider = new VSBooCodeProvider(GetReferences(refs).ToArray(), file);
            _references = refs;
            _file = file;
            refs.OnChildAdded += RefsOnOnChildListChanged;
            refs.OnChildRemoved += RefsOnOnChildListChanged;
        }

        private void RefsOnOnChildListChanged(object sender, HierarchyNodeEventArgs hierarchyNodeEventArgs)
        {
            _provider.References = GetReferences(_references);
        }

        public object CodeDomProvider
        {
            get { return _provider; }
        }
    }
}
