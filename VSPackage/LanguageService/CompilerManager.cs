//
//   Copyright © 2010 Michael Feingold
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Hill30.BooProject.Project;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Hill30.Boo.ASTMapper;

namespace Hill30.BooProject.Compilation
{
    public class CompilerManager
    {
        private readonly List<IFileNode> _compileList = new List<IFileNode>();
        private readonly BooProjectNode _projectManager;
        private readonly Dictionary<uint, AssemblyEntry> _references = new Dictionary<uint, AssemblyEntry>();
        private IDisposable _typeResolverContext;
        private ITypeResolutionService _typeResolver;
        private bool _referencesDirty;

        public CompilerManager(BooProjectNode projectManager)
        {
            _projectManager = projectManager;
            _references.Add((uint)VSConstants.VSITEMID.Root, new AssemblyEntry(new AssemblyName("mscorlib"), _projectManager));
        }

        public void Initialize()
        {
            GlobalServices.TypeService.AssemblyRefreshed += AssemblyRefreshed;
            GlobalServices.TypeService.AssemblyObsolete += AssemblyObsolete;
            GlobalServices.TypeService.AssemblyDeleted += AssemblyDeleted;
        }

        private void ResetAssemblyReferences(Assembly assembly)
        {
            foreach (var reference in _references.Values)
                reference.Refresh(assembly);
            UpdateReferences();
        }

        private void AssemblyRefreshed(object sender, AssemblyRefreshedEventArgs args)
        {
            ResetAssemblyReferences(args.RefreshedAssembly);
        }

        private void AssemblyObsolete(object sender, AssemblyObsoleteEventArgs args)
        {
            ResetAssemblyReferences(args.ObsoleteAssembly);
        }

        void AssemblyDeleted(object sender, AssemblyDeletedEventArgs args)
        {
            ResetAssemblyReferences(args.DeletedAssembly);
        }

        internal void AddReference(ReferenceNode referenceNode)
        {
            _references.Add(referenceNode.ID, new AssemblyEntry(referenceNode, _projectManager));
            UpdateReferences();
        }

		class AssemblyEntry
        {
            private readonly ReferenceNode _reference;
            private Assembly _assembly;
            readonly AssemblyName _assemblyName;
			readonly BooProjectNode _project;

            public AssemblyEntry(AssemblyName assemblyName, BooProjectNode project)
            {
                _assemblyName = assemblyName;
				_project = project;
			}

            public AssemblyEntry(ReferenceNode reference, BooProjectNode project)
            {
                _reference = reference;
				_project = project;
            }

			private AssemblyName GetAssemblyName()
            {
                var assemblyReference = _reference as AssemblyReferenceNode;
                if (assemblyReference != null)
                    return assemblyReference.AssemblyName;

                var projectReference = _reference as ProjectReferenceNode;
                if (projectReference != null)
                    // Now get the name of the assembly from the project.
                    // Some project system throw if the property does not exist. We expect an ArgumentException.
                    try
                    {
						var assemblyNameProperty = projectReference.ReferencedProjectObject.Properties.Item("OutputFileName");
// ReSharper disable AssignNullToNotNullAttribute
                        return new AssemblyName(Path.GetFileNameWithoutExtension(assemblyNameProperty.Value.ToString()));
// ReSharper restore AssignNullToNotNullAttribute
                    }
                    catch (ArgumentException)
                    {
                    }
                return null;
            }

			private Assembly GetAssemblyFromProjectRef(ProjectReferenceNode projectReference)
			{
				var project = projectReference.ReferencedProjectObject;
				var groups = project.ConfigurationManager.ActiveConfiguration.OutputGroups;
				var group = groups.OfType<EnvDTE.OutputGroup>().Single(g => g.CanonicalName == "Built");
				var filenames = ((object[])group.FileURLs).Cast<string>().ToArray();
				var filename = filenames.Single(f => f.EndsWith(".dll") || f.EndsWith(".exe"));
				var lastSlash = filename.LastIndexOf('/');
				if (lastSlash >= 0)
					filename = filename.Substring(lastSlash + 1);
				return Assembly.LoadFile(filename);
			}

			public void Refresh(Assembly target)
            {
                if (_assembly != null && _assembly.FullName == target.FullName)
                    _assembly = null;// target;
            }
            
            public Assembly GetAssembly(Func<AssemblyName, Assembly> assemblyResolver)
            { 
                if (_assembly == null)
                {
                    var aName = _assemblyName ?? GetAssemblyName();
					if (aName != null)
					{
						_assembly = (Assembly)Application.Current.Dispatcher.Invoke(assemblyResolver, aName);
					}
					else if (_reference is ProjectReferenceNode)
					{
						_assembly = GetAssemblyFromProjectRef((ProjectReferenceNode)_reference);
					}
                }
                return _assembly;
            } 
        }

        internal void RemoveReference(ReferenceNode referenceNode)
        {
            _references.Remove(referenceNode.ID);
            UpdateReferences();
        }

        private void UpdateReferences()
        {
            var sources = GlobalServices.LanguageService.GetSources();
            lock (_compileList)
            {
                lock (sources)
                {
                    foreach (Source source in sources)
                        source.IsDirty = true;
                }
                _referencesDirty = true;
            }
        }

        internal void SubmitForCompile(IFileNode file)
        {
            if (_projectManager.IsCodeFile(file.Url) && file.ItemName == "Compile")
                lock (_compileList)
                {
                    _compileList.Add(file);
                }
        }

        public void Compile()
        {

            List<IFileNode> localCompileList;
            bool recompileAll;
            lock (_compileList)
            {
                if (_typeResolverContext == null)
                {
                    Action itr = () =>
                    {
                        _typeResolverContext = GlobalServices.TypeService.GetContextTypeResolver(_projectManager);
                        _typeResolver = GlobalServices.TypeService.GetTypeResolutionService(_projectManager);
                        //hack to ensure that necessary internal state gets initialized
                        _typeResolver.GetPathOfAssembly(new AssemblyName("mscorlib.dll"));
                    };
                    Application.Current.Dispatcher.Invoke(itr);
                }
                localCompileList = new List<IFileNode>(_compileList);
                _compileList.Clear();
                if (localCompileList.Count == 0 && !_referencesDirty)
                    return;
                recompileAll = _referencesDirty;
                _referencesDirty = false;
            }

            var results = new Dictionary<IFileNode, CompileResults>();
            foreach (var file in BooProjectNode.GetFileEnumerator(_projectManager))
                if (recompileAll || localCompileList.Contains(file))
                {
                    // this seemingly redundant variable ensures that each closure below has its own copy of
                    // the file reference. Without it they share the same copy decalred in the loop statemenet
                    // essentially all of them will point to the last element in the loop
                    var localfile = file;
                    results.Add(file, new CompileResults(() => localfile.Url, localfile.GetCompilerInput, ()=>GlobalServices.LanguageService.GetLanguagePreferences().TabSize));
                }
                else
                    results.Add(file, file.GetCompileResults());

			var resolver = BuildAssemblyResolver();
			AppDomain.CurrentDomain.AssemblyResolve += resolver;
			try
			{
				Boo.ASTMapper.CompilerManager.Compile(
					GlobalServices.LanguageService.GetLanguagePreferences().TabSize,
					_references.Values.Select(ae => ae.GetAssembly(_typeResolver.GetAssembly)).Where(a => a != null),
					results.Values);
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= resolver;
			}

            foreach (var result in results)
                result.Key.SetCompilerResults(result.Value);

        }

		private ResolveEventHandler BuildAssemblyResolver()
		{
			return (s, args) => {
				if (args.RequestingAssembly == null) return null;
				var loc = Path.GetDirectoryName(args.RequestingAssembly.Location);
				var asmName = args.Name.Split(',')[0] + ".dll";
				var filename = Path.Combine(loc, asmName);
				if (File.Exists(filename))
					return Assembly.LoadFrom(filename);
				asmName = Path.ChangeExtension(asmName, ".exe");
				filename = Path.Combine(loc, asmName);
				if (File.Exists(filename))
					return Assembly.LoadFrom(filename);
				return null;
			};
		}

		public void Dispose()
        {
            if (_typeResolverContext != null)
                _typeResolverContext.Dispose();
            GlobalServices.TypeService.AssemblyRefreshed -= AssemblyRefreshed;
            GlobalServices.TypeService.AssemblyObsolete -= AssemblyObsolete;
            GlobalServices.TypeService.AssemblyDeleted -= AssemblyDeleted;
        }

    }
}
