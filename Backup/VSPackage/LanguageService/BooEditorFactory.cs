using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Hill30.BooProject.LanguageService
{
    [ProvideView(LogicalView.Code, null)]
    [ProvideView(LogicalView.Designer, "Design")]
    [Guid(Constants.GuidBooEditorFactoryString)]
    class BooEditorFactory : IVsEditorFactory
    {
        private BooProjectPackage _package;
        private ServiceProvider _serviceProvider;

        public BooEditorFactory(BooProjectPackage package)
        {
            _package = package;
        }

        #region IVsEditorFactory implementation
        public int CreateEditorInstance(
                uint createEditorFlags,
                string documentMoniker,
                string physicalView,
                IVsHierarchy hierarchy,
                uint itemid,
                System.IntPtr docDataExisting,
                out System.IntPtr docView,
                out System.IntPtr docData,
                out string editorCaption,
                out Guid commandUIGuid,
                out int createDocumentWindowFlags)

        {
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            editorCaption = null;
            commandUIGuid = new Guid(Constants.GuidBooEditorFactoryString);
            createDocumentWindowFlags = 0;

            // Validate inputs
            if ((createEditorFlags & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
                return VSConstants.E_INVALIDARG;

            IVsTextLines textLines = GetTextBuffer(docDataExisting);

            // Assign docData IntPtr to either existing docData or the new text buffer
            if (docDataExisting != IntPtr.Zero)
            {
                docData = docDataExisting;
                Marshal.AddRef(docData);
            }
            else
            {
                docData = Marshal.GetIUnknownForObject(textLines);
            }

            try
            {
                docView = CreateDocumentView(physicalView, hierarchy, itemid, textLines, out editorCaption, ref commandUIGuid);
            }
            finally
            {
                if (docView == IntPtr.Zero && docDataExisting != docData && docData != IntPtr.Zero)
                {
                    // Cleanup the instance of the docData that we have addref'ed
                    Marshal.Release(docData);
                    docData = IntPtr.Zero;
                }
            }

            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid logicalView, out string physicalView)
        {
            if (logicalView == VSConstants.LOGVIEWID_Primary)
            {
                physicalView = null;
                return VSConstants.S_OK;
            }
            else if (logicalView == VSConstants.LOGVIEWID_Designer)
            {
                physicalView = "Design";
                return VSConstants.S_OK;
            }
            else
            {
                physicalView = null;
                return VSConstants.E_NOTIMPL;
            }
        }

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            _serviceProvider = new ServiceProvider(psp);
            return VSConstants.S_OK;
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsEditorFactory helper methods
        private IVsTextLines GetTextBuffer(System.IntPtr docDataExisting)
        {
            IVsTextLines textLines;
            if (docDataExisting == IntPtr.Zero)
            {
                // Create a new IVsTextLines buffer.
                Type textLinesType = typeof(IVsTextLines);
                Guid riid = textLinesType.GUID;
                Guid clsid = typeof(VsTextBufferClass).GUID;
                textLines = _package.CreateInstance(ref clsid, ref riid, textLinesType) as IVsTextLines;

                // set the buffer's site
                ((IObjectWithSite)textLines).SetSite(_serviceProvider.GetService(typeof(IOleServiceProvider)));
            }
            else
            {
                // Use the existing text buffer
                Object dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
                textLines = dataObject as IVsTextLines;
                if (textLines == null)
                {
                    // Try get the text buffer from textbuffer provider
                    IVsTextBufferProvider textBufferProvider = dataObject as IVsTextBufferProvider;
                    if (textBufferProvider != null)
                    {
                        textBufferProvider.GetTextBuffer(out textLines);
                    }
                }
                if (textLines == null)
                {
                    // Unknown docData type then, so we have to force VS to close the other editor.
                    ErrorHandler.ThrowOnFailure((int)VSConstants.VS_E_INCOMPATIBLEDOCDATA);
                }

            }
            return textLines;
        }

        private IntPtr CreateDocumentView(
            string physicalView,
            IVsHierarchy hierarchy,
            uint itemid,
            IVsTextLines textLines,
            out string editorCaption,
            ref Guid cmdUI)
        {
            //Init out params
            editorCaption = string.Empty;

            if (string.IsNullOrEmpty(physicalView))
            {
                // create code window as default physical view
                return CreateCodeView(textLines, out editorCaption, out cmdUI);
            }
            else if (string.Compare(physicalView, "design", true, CultureInfo.InvariantCulture) == 0)
            {
                // Create Form view
                return CreateFormView(hierarchy, itemid, textLines, ref editorCaption, ref cmdUI);
            }

            // We couldn't create the view
            // Return special error code so VS can try another editor factory.
            cmdUI = Guid.Empty;
            ErrorHandler.ThrowOnFailure((int)VSConstants.VS_E_UNSUPPORTEDFORMAT);
            return IntPtr.Zero;
        }

        public virtual object GetService(Type serviceType)
        {
            // This is were we will load the IVSMDProvider interface
            return _serviceProvider.GetService(serviceType);
        }

        private IntPtr CreateFormView(
            IVsHierarchy hierarchy,
            uint itemid,
            IVsTextLines textLines,
            ref string editorCaption,
            ref Guid cmdUI)
        {
            // Request the Designer Service
            IVSMDDesignerService designerService = (IVSMDDesignerService)GetService(typeof(IVSMDDesignerService));

            // Create loader for the designer
            IVSMDDesignerLoader designerLoader =
                (IVSMDDesignerLoader)designerService.CreateDesignerLoader(
                    "Microsoft.VisualStudio.Designer.Serialization.VSDesignerLoader");

            var loaderInitalized = false;
            try
            {
                var service = _serviceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider;

                // Initialize designer loader 
                designerLoader.Initialize(service, hierarchy, (int)itemid, textLines);
                loaderInitalized = true;

                // Create the designer
                IVSMDDesigner designer = designerService.CreateDesigner(service, designerLoader);

                // Get editor caption
                editorCaption = designerLoader.GetEditorCaption((int)READONLYSTATUS.ROSTATUS_Unknown);

                // Get view from designer
                object docView = designer.View;

                // Get command guid from designer
                cmdUI = designer.CommandGuid;

                return Marshal.GetIUnknownForObject(docView);
            }
            catch
            {
                // The designer loader may have created a reference to the shell or the text buffer.
                // In case we fail to create the designer we should manually dispose the loader
                // in order to release the references to the shell and the textbuffer
                if (loaderInitalized)
                {
                    designerLoader.Dispose();
                }
                throw;
            }
        }

        private IntPtr CreateCodeView(IVsTextLines textLines, out string editorCaption, out Guid cmdUI)
        {
            var codeWindowType = typeof(IVsCodeWindow);
            var riid = codeWindowType.GUID;
            var clsid = typeof(VsCodeWindowClass).GUID;
            var window = (IVsCodeWindow)_package.CreateInstance(ref clsid, ref riid, codeWindowType);

            ErrorHandler.ThrowOnFailure(window.SetBuffer(textLines));
            ErrorHandler.ThrowOnFailure(window.SetBaseEditorCaption(null));
            ErrorHandler.ThrowOnFailure(window.GetEditorCaption(READONLYSTATUS.ROSTATUS_Unknown, out editorCaption));

            cmdUI = VSConstants.GUID_TextEditorFactory;
            return Marshal.GetIUnknownForObject(window);
        }

        #endregion
    }
}
