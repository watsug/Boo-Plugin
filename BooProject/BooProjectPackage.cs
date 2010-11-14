﻿//
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
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;

namespace Hill30.BooProject
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "0.5.0.2", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]

    // Registers boo project factory
    [ProvideProjectFactory(typeof(Project.BooProjectFactory), "Visual Boo", "Boo Project Files (*.booproj);*.booproj", "booproj", "booproj", @".\NullPath", LanguageVsTemplate = "Visual Boo", NewProjectRequireNewFolderVsTemplate = false)]
    [ProvideProjectItem(typeof(Project.BooProjectFactory), "Boo", @"Templates\Items\Boo", 500)]

    // Registers property pages for boo project designer
    [ProvideObject(typeof(Project.ProjectProperties.Application))]
    [ProvideObject(typeof(Project.ProjectProperties.Build))]

    // Registers the language service
    [ProvideService(typeof(LanguageService.BooLanguageService))]
    [ProvideLanguageExtension(typeof(LanguageService.BooLanguageService), ".boo")]

    [ProvideLanguageServiceAttribute(typeof(LanguageService.BooLanguageService),
                             Constants.LanguageName,
                             106,                           // resource ID of localized language name
                             CodeSense = true,             // Supports IntelliSense
                             RequestStockColors = false,     // Does not supply custom colors
                             EnableCommenting = true,       // Supports commenting out code
                             EnableAsyncCompletion = true   // Supports background parsing
                             
                             )]

    [Guid(Constants.GuidBooProjectPkgString)]
    public sealed class BooProjectPackage : ProjectPackage
    {
 
        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            RegisterProjectFactory(new Project.BooProjectFactory(this));

            LanguageService.BooLanguageService.Register(this);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                var menuCommandID = new CommandID(Constants.GuidBooProjectCmdSet, (int)PkgCmdIDList.cmdidMyCommand);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand( menuItem );
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LanguageService.BooLanguageService.Stop(this);
            }
            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            //var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //Guid clsid = Guid.Empty;
            //int result;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            //           0,
            //           ref clsid,
            //           "BooProject",
            //           string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", ToString()),
            //           string.Empty,
            //           0,
            //           OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //           OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            //           OLEMSGICON.OLEMSGICON_INFO,
            //           0,        // false
            //           out result));
        }

        public override string ProductUserContext
        {
            get { return "Boo Project"; }
        }
    }
}
