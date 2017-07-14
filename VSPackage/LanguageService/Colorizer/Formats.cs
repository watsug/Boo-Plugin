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
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows.Media;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Hill30.BooProject.LanguageService.Colorizer
{
    class Formats
    {

        internal const string BooKeyword = "Boo Keyword";
        [Export]
        [Name(BooKeyword)]
        private static ClassificationTypeDefinition booKeyword;

        [Export(typeof(EditorFormatDefinition))]
        [Name(BooKeyword)]
        [DisplayName("Boo Keyword Format")]
        [UserVisible(true)]
        [ClassificationType(ClassificationTypeNames = BooKeyword)]
        [Order]
        internal sealed class BooKeywordFormat : ClassificationFormatDefinition
        {
            public BooKeywordFormat()
            {
                ForegroundColor = Colors.Blue;
                BackgroundColor = Colors.Beige;
            }
        }

        internal const string BooBlockComment = "Boo Comment";
        [Export]
        [Name(BooBlockComment)]
        private static ClassificationTypeDefinition booBlockComment;

        [Export(typeof(EditorFormatDefinition))]
        [Name(BooBlockComment)]
        [DisplayName("Boo Comment Format")]
        [UserVisible(true)]
        [ClassificationType(ClassificationTypeNames = BooBlockComment)]
        [Order]
        internal sealed class BooBlockCommentFormat : ClassificationFormatDefinition
        {
            public BooBlockCommentFormat()
            {
                ForegroundColor = Colors.Green;
            }
        }

        internal const string BooType = "Boo Type";
        [Export]
        [Name(BooType)]
        private static ClassificationTypeDefinition booType;

        [Export(typeof(EditorFormatDefinition))]
        [Name(BooType)]
        [DisplayName("Boo Type Format")]
        [UserVisible(true)]
        [ClassificationType(ClassificationTypeNames = BooType)]
        [Order]
        private sealed class BooTypeFormat : ClassificationFormatDefinition
        {
            public BooTypeFormat()
            {
                ForegroundColor = Colors.SteelBlue;
            }
        }

        internal const string BooMacro = "Boo Macro";
        [Export]
        [Name(BooMacro)]
        private static ClassificationTypeDefinition booMacro;

        [Export(typeof(EditorFormatDefinition))]
        [Name(BooMacro)]
        [DisplayName("Boo Macro Format")]
        [UserVisible(true)]
        [ClassificationType(ClassificationTypeNames = BooMacro)]
        [Order]
        internal sealed class BooMacroFormat : ClassificationFormatDefinition
        {
            public BooMacroFormat()
            {
                ForegroundColor = Colors.Fuchsia;
            }
        }
    }
}
