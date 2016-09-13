using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.CodeDom;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Project;

namespace Hill30.BooProject.LanguageService
{
    class VSMDBooCodeProvider : IVSMDCodeDomProvider
    {
        private readonly BooCodeProvider _provider;
        private readonly ReferenceContainerNode _references;

        private IEnumerable<string> GetReferences(ReferenceContainerNode refs)
        {
            var node = refs.FirstChild;
            while (node != null)
            {
                yield return node.Caption;
                node = node.NextSibling;
            }
        }

        public VSMDBooCodeProvider(ReferenceContainerNode refs)
        {
            _provider = new BooCodeProvider(GetReferences(refs).ToArray());
            _references = refs;
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
