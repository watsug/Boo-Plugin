﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Hill30.BooProject.LanguageService.Colorizer;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Hill30.BooProject.LanguageService
{
    public class BooLanguageService : Microsoft.VisualStudio.Package.LanguageService, IOleComponent
    {
        [Import]
        public IVsEditorAdaptersFactoryService BufferAdapterService { get; private set; }

        [Import]
        public IClassificationTypeRegistryService ClassificationTypeRegistry { get; private set; }

        internal static void Register(IServiceContainer container)
        {
            // Proffer the service.
            var langService = new BooLanguageService();
            langService.SetSite(container);

            var m = ((IComponentModel)langService.GetService(typeof(SComponentModel))).DefaultCompositionService;
            m.SatisfyImportsOnce(langService);

            container.AddService(typeof(BooLanguageService), langService, true);
            langService.Start();
        }

        internal static void Stop(IServiceContainer container)
        {
            var service = container.GetService(typeof(BooLanguageService))
                                      as BooLanguageService;
            if (service == null || service.mComponentID == 0)
                return;

            var mgr = container.GetService(typeof(SOleComponentManager))
                                       as IOleComponentManager;
            if (mgr != null)
            {
                mgr.FRevokeComponent(service.mComponentID);
            }
            service.mComponentID = 0;
        }

        public override string GetFormatFilterList()
        {
            return "Boo files(*.boo)|*.boo";
        }

        private LanguagePreferences mPreferences;

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (mPreferences == null)
            {
                mPreferences = new LanguagePreferences(Site, typeof(BooLanguageService).GUID, Name );
                mPreferences.Init();
            }
            return mPreferences;
        }

        public override IScanner GetScanner(IVsTextLines buffer)
        {
            return new Scanner.Scanner(this, buffer);
        }

        public override string Name
        {
            get { return Constants.LanguageName; }
        }

        public override Source CreateSource(IVsTextLines buffer)
        {
            return new BooSource(this, buffer, GetColorizer(buffer)) {LastParseTime = 0};
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            var source = GetSource(req.View) as BooSource;

            if (source != null && req.Reason == ParseReason.Check)
                source.Compile(req);

            return new BooAuthoringScope(source);
        }

        public override int GetItemCount(out int count)
        {
            count = Formats.ColorableItems.Length - 1;
            return VSConstants.S_OK;
        }

        public override int GetColorableItem(int index, out IVsColorableItem item)
        {
            item = Formats.ColorableItems[index];
            return VSConstants.S_OK;
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView)
        {
            return new BooTypeAndMemberDropdownBars(this, (BooSource)GetSource(forView));
        }

        private void Start()
        {
            // Register a timer to call our language service during
            // idle periods.
            var mgr = Site.GetService(typeof(SOleComponentManager)) as IOleComponentManager;

            if (mComponentID == 0 && mgr != null)
            {
                var crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime |
                                              (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
                crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal |
                                              (uint)_OLECADVF.olecadvfRedrawOff |
                                              (uint)_OLECADVF.olecadvfWarningsOff;
                crinfo[0].uIdleTimeInterval = 100;
                mgr.FRegisterComponent(this, crinfo, out mComponentID);
            }
        }

        private uint mComponentID;

        #region IOleComponent Members

        public int FDoIdle(uint grfidlef)
        {
            bool bPeriodic = (grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0;
            // Use typeof(TestLanguageService) because we need to
            // reference the GUID for our language service.
            var service = GetService(typeof(BooLanguageService))
                                      as BooLanguageService;
            if (service != null)
            {
                service.OnIdle(bPeriodic);
            }
            return 0;
        }

        public int FContinueMessageLoop(uint uReason,
                                        IntPtr pvLoopData,
                                        MSG[] pMsgPeeked)
        {
            return 1;
        }

        public int FPreTranslateMessage(MSG[] pMsg)
        {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser)
        {
            return 1;
        }

        public int FReserved1(uint dwReserved,
                              uint message,
                              IntPtr wParam,
                              IntPtr lParam)
        {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
        {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic,
                                       int fSameComponent,
                                       OLECRINFO[] pcrinfo,
                                       int fHostIsActivating,
                                       OLECHOSTINFO[] pchostinfo,
                                       uint dwReserved)
        {
        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID)
        {
        }

        public void OnEnterState(uint uStateID, int fEnter)
        {
        }

        public void OnLoseActivation()
        {
        }

        public void Terminate()
        {
        }

        #endregion
    }
}