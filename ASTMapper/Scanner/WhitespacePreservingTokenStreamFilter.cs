using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using antlr;
using System.Collections;
using Boo.Lang.Parser;

namespace Hill30.Boo.ASTMapper.Scanner
{

    class WhitespacePreservingTokenStreamFilter : TokenStream
    {
        static readonly char[] NewLineCharArray = new char[] { '\r', '\n' };

        private TokenStream _istream;
        private Queue _pendingTokens;
        private int _endTokenType;
        private int _idTokenType;
        private int _eosTokenType;
        StringBuilder _buffer = new StringBuilder();

        /// <summary>
        /// first detected indentation character
        /// </sumary>
        protected string _expectedIndent;

        internal WhitespacePreservingTokenStreamFilter(antlr.TokenStream istream, int eosType, int endType, int idType)
        {
            if (istream == null)
            {
                throw new ArgumentNullException("istream");
            }

            _istream = istream;
            _pendingTokens = new Queue();
            _eosTokenType = eosType;
            _endTokenType = endType;
            _idTokenType = idType;
        }

        void ResetBuffer()
        {
            _buffer.Length = 0;
        }

        public IToken nextToken()
        {
            if (_pendingTokens.Count == 0)
                ProcessNextTokens();
            IToken token = (IToken)_pendingTokens.Dequeue();
            // In non-wsa mode `end` is just another identifier
            if (token.Type == _endTokenType)
            {
                token.Type = _idTokenType;
            }
            return token;
        }

        IToken ReadAndBuffer()
        {
            IToken token = null;
            while (true)
            {
                token = _istream.nextToken();

                int ttype = token.Type;
                if (ttype == Token.SKIP)
                    continue;

                break;
            }
            return token;
        }

        void FlushBuffer(IToken token)
        {
            if (0 == _buffer.Length) return;

            string text = _buffer.ToString();
            string[] lines = text.Split(NewLineCharArray);

            if (lines.Length > 1)
            {
                string lastLine = lines[lines.Length - 1];

                // Protect against mixed indentation issues
                if (String.Empty != lastLine)
                {
                    if (null == _expectedIndent)
                    {
                        _expectedIndent = lastLine.Substring(0, 1);
                    }

                    if (String.Empty != lastLine.Replace(_expectedIndent, String.Empty))
                    {
                        string literal = _expectedIndent == "\t"
                                       ? "tabs"
                                       : _expectedIndent == "\f"
                                       ? "form feeds"  // The lexer allows them :p
                                       : "spaces";

                        throw new TokenStreamRecognitionException(
                            new RecognitionException(
                                "Mixed indentation, expected the use of " + literal,
                                token.getFilename(),
                                token.getLine(),
                                // Point exactly to the first invalid char
                                lastLine.Length - lastLine.TrimStart(_expectedIndent[0]).Length + 1
                            )
                        );
                    }
                }

            }
        }

        IToken CreateToken(IToken prototype, int newTokenType, string newTokenText)
        {
            return new BooToken(newTokenType, newTokenText,
                prototype.getFilename(),
                prototype.getLine(),
                prototype.getColumn() + SafeGetLength(prototype.getText()));
        }

        int SafeGetLength(string s)
        {
            return s == null ? 0 : s.Length;
        }

        void EnqueueEOS(IToken prototype)
        {
            _pendingTokens.Enqueue(CreateToken(prototype, _eosTokenType, "<EOL>"));
        }

        void CheckForEOF(IToken token)
        {
            if (token.Type != Token.EOF_TYPE) return;

            EnqueueEOS(token);
        }

        void ProcessNextTokens()
        {
            ResetBuffer();

            IToken token = ReadAndBuffer();
            FlushBuffer(token);
            CheckForEOF(token);
            _pendingTokens.Enqueue(token);
        }

    }
}
