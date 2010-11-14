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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;

namespace Hill30.BooProject.LanguageService.Colorizer
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("Visual Boo")]
    [Name("Boo Classifier")]
    internal class ClassifierProvider : IClassifierProvider
    {
        [Import] 
        private IVsEditorAdaptersFactoryService bufferAdapterService;

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider serviceProvider;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new Classifier(
                (BooLanguageService)serviceProvider.GetService(typeof(BooLanguageService)), 
                (IVsTextLines)bufferAdapterService.GetBufferAdapter(textBuffer));
        }
    }
}
