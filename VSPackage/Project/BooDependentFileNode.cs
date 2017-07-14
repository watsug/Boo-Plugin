using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Hill30.Boo.ASTMapper;
using Hill30.Boo.ASTMapper.AST;
using Hill30.Boo.ASTMapper.AST.Nodes;
using Hill30.BooProject.LanguageService;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Hill30.BooProject.Project
{
    public class BooDependentFileNode : DependentFileNode, IFileNode
    {

        private CompileResults results;
        private ITextBuffer textBuffer;
        private ITextSnapshot originalSnapshot;
        private bool hidden;
        private readonly BooLanguageService languageService;

        private VSMDBooCodeProvider CodeDomProvider(ProjectNode project)
        {
            var child = project.FirstChild;
            while (!(child is ReferenceContainerNode))
                child = child.NextSibling;
            return new VSMDBooCodeProvider((ReferenceContainerNode)child, this);
        }

        public BooDependentFileNode(ProjectNode root, ProjectElement e)
			: base(root, e)
		{
            results = new CompileResults(() => Url, GetCompilerInput, ()=>GlobalServices.LanguageService.GetLanguagePreferences().TabSize);
            languageService = (BooLanguageService)GetService(typeof(BooLanguageService));
            hidden = true;
            this.OleServiceProvider.AddService(typeof(IVSMDCodeDomProvider), CodeDomProvider(root), false);
        }

        protected override void DoDefaultAction()
        {
            FileDocumentManager manager = this.GetDocumentManager() as FileDocumentManager;
            Guid viewGuid = (this.HasDesigner ? VSConstants.LOGVIEWID_Designer : VSConstants.LOGVIEWID_Code);
            IVsWindowFrame frame;
            Application.Current.Dispatcher.Invoke(
                () => manager.Open(false, false, viewGuid, out frame, WindowFrameShowAction.Show));
        }

        public CompileResults GetCompileResults()
        {
            return results;
        }

        public void SetCompilerResults(CompileResults newResults)
        {
            results.HideMessages(((BooProjectNode)ProjectMgr).RemoveTask);
            results = newResults;
            if (!hidden)
                results.ShowMessages(((BooProjectNode)ProjectMgr).AddTask, Navigate);
            if (Recompiled != null)
                Recompiled(this, EventArgs.Empty);
        }

        public IEnumerable<MappedTypeDefinition> Types { get { return GetCompileResults().Types; } }

        public event EventHandler Recompiled;

        public CompileResults.BufferPoint MapPosition(int line, int column) { return GetCompileResults().LocationToPoint(line, column); }

        public void HideMessages() { GetCompileResults().HideMessages(((BooProjectNode)ProjectMgr).RemoveTask); }

        public void ShowMessages() { GetCompileResults().ShowMessages(((BooProjectNode)ProjectMgr).AddTask, Navigate); }

        public void SubmitForCompile() { ((BooProjectNode)ProjectMgr).SubmitForCompile(this); }

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans) { return GetCompileResults().GetTags(spans, SnapshotCreator); }

        public MappedToken GetMappedToken(int line, int col) { return GetCompileResults().GetMappedToken(line, col); }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return GetCompileResults().GetClassificationSpans(languageService.ClassificationTypeRegistry, span, SnapshotCreator);
        }

        public MappedToken GetAdjacentMappedToken(int line, int col) { return GetCompileResults().GetAdjacentMappedToken(line, col); }

        public void Bind(ITextBuffer buffer)
        {
            textBuffer = buffer;
            if (buffer == null)
                hidden = true;
            else
                originalSnapshot = buffer.CurrentSnapshot;
        }

        private SnapshotSpan SnapshotCreator(TextSpan textspan)
        {
            if (textBuffer == null)
                return default(SnapshotSpan);

            var startIndex = originalSnapshot.GetLineFromLineNumber(textspan.iStartLine).Start + textspan.iStartIndex;
            var endLine = originalSnapshot.GetLineFromLineNumber(textspan.iEndLine);
            return
                textspan.iEndIndex == -1
                ? new SnapshotSpan(originalSnapshot, startIndex, endLine.Start + endLine.Length - startIndex)
                : new SnapshotSpan(originalSnapshot, startIndex, endLine.Start + textspan.iEndIndex - startIndex);
        }

        public string ItemName
        {
            get { return ItemNode.ItemName; }
        }

        private void Navigate(ErrorTask target)
        {
            ProjectMgr.Navigate(target.Document, target.Line, target.Column);
        }

        public string GetCompilerInput()
        {
            string source;
            if (textBuffer == null)
                source = File.ReadAllText(Url);
            else
            {
                hidden = false;
                originalSnapshot = textBuffer.CurrentSnapshot;
                source = originalSnapshot.GetText();
            }
            return source;
        }


        internal OleServiceProvider.ServiceCreatorCallback ServiceCreator
        {
            get { return CreateServices; }
        }

        private BooProjectNode GetProjectReferences()
        {
            HierarchyNode parentNode = this.Parent;
            while (!(parentNode is BooProjectNode))
                parentNode = parentNode.Parent;
            return (BooProjectNode)parentNode;
        }

        private object CreateServices(Type serviceType)
        {
            if (typeof(EnvDTE.ProjectItem) == serviceType)
            {
                return GetAutomationObject();
            }
            if (typeof(SVSMDCodeDomProvider) == serviceType)
            {
                return CodeDomProvider(this.ProjectMgr);
            }
            return null;
        }
        

    }
}
