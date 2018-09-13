using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Runtime;

namespace Boo.Lang.CodeDom
{

    [Serializable]
    public class BooCodeDomConverter : FastDepthFirstVisitor
    {
        private CodeCompileUnit _codeDomUnit;

        private CodeNamespace _defaultNS;

        private CodeTypeDeclaration _currentType;

        private CodeTypeMember _currentMember;

        private CodeExpressionStatement _currentStatement;

        private CodeStatementCollection _currentBlock;

        [NonSerialized]
        protected static Hash _primitives = new Hash
    {
        {
            (object)"double",
            (object)"System.Double"
        },
        {
            (object)"single",
            (object)"System.Single"
        },
        {
            (object)"int",
            (object)"System.Int32"
        },
        {
            (object)"long",
            (object)"System.Int64"
        },
        {
            (object)"short",
            (object)"System.Int16"
        },
        {
            (object)"ushort",
            (object)"System.UInt16"
        },
        {
            (object)"byte",
            (object)"System.Byte"
        },
        {
            (object)"bool",
            (object)"System.Boolean"
        },
        {
            (object)"char",
            (object)"System.Char"
        },
        {
            (object)"string",
            (object)"System.String"
        },
        {
            (object)"object",
            (object)"System.Object"
        },
        {
            (object)"void",
            (object)"System.Void"
        },
        {
            (object)"date",
            (object)"System.DateTime"
        },
        {
            (object)"timespan",
            (object)"System.TimeSpan"
        }
    };

        [NonSerialized]
        private static readonly Hash OPS = new Hash
    {
        {
            (object)BinaryOperatorType.Addition,
            (object)CodeBinaryOperatorType.Add
        },
        {
            (object)BinaryOperatorType.Subtraction,
            (object)CodeBinaryOperatorType.Subtract
        },
        {
            (object)BinaryOperatorType.Multiply,
            (object)CodeBinaryOperatorType.Multiply
        },
        {
            (object)BinaryOperatorType.Division,
            (object)CodeBinaryOperatorType.Divide
        },
        {
            (object)BinaryOperatorType.Modulus,
            (object)CodeBinaryOperatorType.Modulus
        },
        {
            (object)BinaryOperatorType.LessThan,
            (object)CodeBinaryOperatorType.LessThan
        },
        {
            (object)BinaryOperatorType.LessThanOrEqual,
            (object)CodeBinaryOperatorType.LessThanOrEqual
        },
        {
            (object)BinaryOperatorType.GreaterThan,
            (object)CodeBinaryOperatorType.GreaterThan
        },
        {
            (object)BinaryOperatorType.GreaterThanOrEqual,
            (object)CodeBinaryOperatorType.GreaterThanOrEqual
        },
        {
            (object)BinaryOperatorType.Equality,
            (object)CodeBinaryOperatorType.ValueEquality
        },
        {
            (object)BinaryOperatorType.ReferenceEquality,
            (object)CodeBinaryOperatorType.IdentityEquality
        },
        {
            (object)BinaryOperatorType.ReferenceInequality,
            (object)CodeBinaryOperatorType.IdentityInequality
        },
        {
            (object)BinaryOperatorType.Or,
            (object)CodeBinaryOperatorType.BooleanOr
        },
        {
            (object)BinaryOperatorType.And,
            (object)CodeBinaryOperatorType.BitwiseAnd
        },
        {
            (object)BinaryOperatorType.BitwiseOr,
            (object)CodeBinaryOperatorType.BitwiseOr
        },
        {
            (object)BinaryOperatorType.BitwiseAnd,
            (object)CodeBinaryOperatorType.BitwiseAnd
        }
    };

        public CodeCompileUnit CodeDomUnit
        {
            get
            {
                return this._codeDomUnit;
            }
        }

        private static CodeTypeReference GetType(string name)
        {
            object result;
            if (BooCodeDomConverter._primitives.ContainsKey(name))
            {
                object obj = ((Hashtable)BooCodeDomConverter._primitives)[name];
                if (!(obj is string))
                {
                    obj = RuntimeServices.Coerce(obj, typeof(string));
                }
                result = new CodeTypeReference((string)obj);
            }
            else
            {
                name = name.Replace("[of ", "[");
                result = new CodeTypeReference(name);
            }
            return (CodeTypeReference)result;
        }

        public override void OnModule(Module node)
        {
            if (!node.Namespace.Name.Equals("CompilerGenerated"))
            {
                base.OnModule(node);
            }
        }

        public override void OnCompileUnit(CompileUnit node)
        {
            this._codeDomUnit = new CodeCompileUnit();
            this._defaultNS = new CodeNamespace();
            this._codeDomUnit.Namespaces.Add(this._defaultNS);
            base.OnCompileUnit(node);
        }

        private MemberAttributes ModifiersToDomAttributes(TypeMemberModifiers value)
        {
            MemberAttributes memberAttributes = default(MemberAttributes);
            MemberAttributes memberAttributes2 = memberAttributes;
            if ((TypeMemberModifiers.Private & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Private;
            }
            if ((TypeMemberModifiers.Internal & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Assembly;
            }
            if ((TypeMemberModifiers.Protected & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Family;
            }
            if ((TypeMemberModifiers.Public & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Public;
            }
            if ((TypeMemberModifiers.Static & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Static;
            }
            if ((TypeMemberModifiers.Virtual & value) == TypeMemberModifiers.None && (TypeMemberModifiers.Override & value) == TypeMemberModifiers.None)
            {
                memberAttributes2 |= MemberAttributes.Final;
            }
            if ((TypeMemberModifiers.Override & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Override;
            }
            if ((TypeMemberModifiers.Abstract & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.Abstract;
            }
            if ((TypeMemberModifiers.New & value) != 0)
            {
                memberAttributes2 |= MemberAttributes.New;
            }
            return memberAttributes2;
        }

        private void VisitAttributes(IEnumerable<Boo.Lang.Compiler.Ast.Attribute> attrs, CodeAttributeDeclarationCollection coll)
        {
            if (attrs != null)
            {
                IEnumerator<Boo.Lang.Compiler.Ast.Attribute> enumerator = attrs.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        Boo.Lang.Compiler.Ast.Attribute current = enumerator.Current;
                        CodeAttributeDeclaration codeAttributeDeclaration = new CodeAttributeDeclaration(current.Name, current.Arguments.Select(this.VisitAttributesClosure3).ToArray());
                        codeAttributeDeclaration.Arguments.AddRange(current.NamedArguments.Select(this.VisitAttributesClosure4).ToArray());
                        coll.Add(codeAttributeDeclaration);
                    }
                }
                finally
                {
                    enumerator.Dispose();
                }
            }
        }

        private void OnTypeMember(TypeMember node, CodeTypeMember domNode)
        {
            domNode.Name = node.Name;
            domNode.Attributes = this.ModifiersToDomAttributes(node.Modifiers);
            CodeTypeMember currentMember = this._currentMember;
            this.VisitAttributes(node.Attributes, domNode.CustomAttributes);
            domNode.UserData["LexicalInfo"] = node.LexicalInfo;
            this._currentMember = currentMember;
        }

        private void OnTypeDefinition(TypeDefinition node)
        {
            CodeTypeDeclaration currentType = this._currentType;
            this._currentType = new CodeTypeDeclaration();
            this.OnTypeMember(node, this._currentType);
            this._currentType.IsPartial = ((TypeMemberModifiers.Partial & node.Modifiers) == TypeMemberModifiers.Partial);
            IEnumerator<TypeReference> enumerator = node.BaseTypes.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    TypeReference current = enumerator.Current;
                    this._currentType.BaseTypes.Add(BooCodeDomConverter.GetType(current.ToString()));
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            this.Visit(node.GenericParameters);
            this.Visit(node.Members);
            if (currentType != null)
            {
                currentType.Members.Add(this._currentType);
            }
            else
            {
                this._defaultNS.Types.Add(this._currentType);
            }
            this._currentType = currentType;
        }

        public override void OnClassDefinition(ClassDefinition node)
        {
            this.OnTypeDefinition(node);
        }

        public override void OnTypeMemberStatement(TypeMemberStatement node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnExplicitMemberInfo(ExplicitMemberInfo node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnArrayTypeReference(ArrayTypeReference node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnGenericTypeDefinitionReference(GenericTypeDefinitionReference node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnCallableDefinition(CallableDefinition node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnNamespaceDeclaration(NamespaceDeclaration node)
        {
            this._defaultNS.Name = node.Name;
        }

        public override void OnImport(Import node)
        {
            this._defaultNS.Imports.Add(new CodeNamespaceImport(node.Namespace));
        }

        public override void OnStructDefinition(StructDefinition node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnEnumMember(EnumMember node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        private CodeExpression VisitExpr(Expression node)
        {
            CodeExpressionStatement currentStatement = this._currentStatement;
            this._currentStatement = new CodeExpressionStatement();
            try
            {
                this.Visit(node);
                return this._currentStatement.Expression;
                IL_002a:
                CodeExpression result;
                return result;
            }
            finally
            {
                this._currentStatement = currentStatement;
            }
        }

        public override void OnField(Field node)
        {
            CodeMemberField codeMemberField = new CodeMemberField();
            this.OnTypeMember(node, codeMemberField);
            if (node.Type != null)
            {
                codeMemberField.Type = BooCodeDomConverter.GetType(node.Type.ToString());
            }
            codeMemberField.InitExpression = this.VisitExpr(node.Initializer);
            this._currentType.Members.Add(codeMemberField);
        }

        public override void OnProperty(Property node)
        {
        }

        public override void OnEvent(Event node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnLocal(Local node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            throw new NotImplementedException();
            IL_0006:;
        }

        private void VisitParameters(IEnumerable<ParameterDeclaration> @params, CodeParameterDeclarationExpressionCollection collection)
        {
            IEnumerator<ParameterDeclaration> enumerator = @params.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    ParameterDeclaration current = enumerator.Current;
                    CodeParameterDeclarationExpression codeParameterDeclarationExpression = new CodeParameterDeclarationExpression(BooCodeDomConverter.GetType(current.Type.ToString()), current.Name);
                    if (current.Modifiers == ParameterModifiers.Ref)
                    {
                        codeParameterDeclarationExpression.Direction = FieldDirection.Ref;
                    }
                    this.VisitAttributes(current.Attributes, codeParameterDeclarationExpression.CustomAttributes);
                    collection.Add(codeParameterDeclarationExpression);
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        private void VisitMethod(Method node, CodeMemberMethod result)
        {
            result.Name = node.Name;
            this.VisitParameters(node.Parameters, result.Parameters);
            if (node.ReturnType != null)
            {
                result.ReturnType = BooCodeDomConverter.GetType(node.ReturnType.ToCodeString());
            }
            this.OnTypeMember(node, result);
            if (node.Name == "InitializeComponent")
            {
                CodeStatementCollection currentBlock = this._currentBlock;
                this._currentBlock = result.Statements;
                this.Visit(node.Body);
                if (node.ImplementationFlags != 0)
                {
                    throw new AssertionFailedException("node.ImplementationFlags == MethodImplementationFlags.None");
                }
                if (node.ExplicitInfo != null)
                {
                    throw new AssertionFailedException("node.ExplicitInfo is null");
                }
                this._currentBlock = currentBlock;
            }
            else
            {
                string[] array = node.Body.ToCodeString().Split(new string[3]
                {
                "\r\n",
                "\r",
                "\n"
                }, StringSplitOptions.None);
                int num = 1;
                Node node2 = node;
                while (node2.ParentNode != null && RuntimeServices.op_Member(node2.ParentNode.NodeType, new NodeType[2]
                {
                NodeType.ClassDefinition,
                NodeType.StructDefinition
                }))
                {
                    num = checked(num + 1);
                    node2 = node2.ParentNode;
                }
                int num2 = 0;
                int length = array.Length;
                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException("max");
                }
                while (num2 < length)
                {
                    int index = num2;
                    num2++;
                    string[] array2 = array;
                    int num3 = RuntimeServices.NormalizeArrayIndex(array2, index);
                    string lhs = new string('\t', num);
                    string[] array3 = array;
                    array2[num3] = lhs + array3[RuntimeServices.NormalizeArrayIndex(array3, index)];
                }
                string value = string.Join("\r\n", array);
                result.Statements.Add(new CodeSnippetStatement(value));
            }
            this._currentType.Members.Add(result);
        }

        public override void OnMethod(Method node)
        {
            if (!node.IsSynthetic)
            {
                this.VisitMethod(node, new CodeMemberMethod());
            }
        }

        public override void OnConstructor(Constructor node)
        {
            if (!node.IsSynthetic)
            {
                this.VisitMethod(node, new CodeConstructor());
            }
        }

        public override void OnDestructor(Destructor node)
        {
            throw new NotImplementedException();
        }

        public override void OnParameterDeclaration(ParameterDeclaration node)
        {
            throw new NotImplementedException();
        }

        public override void OnGenericParameterDeclaration(GenericParameterDeclaration node)
        {
            throw new NotImplementedException();
        }

        public override void OnDeclaration(Declaration node)
        {
            throw new NotImplementedException();
        }

        public override void OnAttribute(Boo.Lang.Compiler.Ast.Attribute node)
        {
            throw new NotImplementedException();
        }

        public override void OnStatementModifier(StatementModifier node)
        {
            throw new NotImplementedException();
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnDeclarationStatement(DeclarationStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnMacroStatement(MacroStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnTryStatement(TryStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnExceptionHandler(ExceptionHandler node)
        {
            throw new NotImplementedException();
        }

        public override void OnIfStatement(IfStatement node)
        {
            CodeConditionStatement codeConditionStatement = new CodeConditionStatement(this.VisitExpr(node.Condition));
            CodeStatementCollection currentBlock = this._currentBlock;
            if (node.TrueBlock != null)
            {
                this._currentBlock = codeConditionStatement.TrueStatements;
                this.Visit(node.TrueBlock);
            }
            if (node.FalseBlock != null)
            {
                this._currentBlock = codeConditionStatement.FalseStatements;
                this.Visit(node.FalseBlock);
            }
            this._currentBlock = currentBlock;
            this._currentBlock.Add(codeConditionStatement);
        }

        public override void OnUnlessStatement(UnlessStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnForStatement(ForStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnBreakStatement(BreakStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnRaiseStatement(RaiseStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnUnpackStatement(UnpackStatement node)
        {
            throw new NotImplementedException();
        }

        private CodeAssignStatement ConvertAssign(BinaryExpression be)
        {
            return new CodeAssignStatement(this.VisitExpr(be.Left), this.VisitExpr(be.Right));
        }

        private CodeAttachEventStatement ConvertAttach(BinaryExpression be)
        {
            return new CodeAttachEventStatement((CodeEventReferenceExpression)this.VisitExpr(be.Left), this.VisitExpr(be.Right));
        }

        public override void OnExpressionStatement(ExpressionStatement node)
        {
            BinaryExpression binaryExpression = node.Expression as BinaryExpression;
            if (binaryExpression != null)
            {
                if (binaryExpression.Operator == BinaryOperatorType.Assign)
                {
                    this._currentBlock.Add(this.ConvertAssign(binaryExpression));
                    return;
                }
                if (binaryExpression.Operator == BinaryOperatorType.InPlaceAddition)
                {
                    this._currentBlock.Add(this.ConvertAttach(binaryExpression));
                    return;
                }
            }
            CodeExpressionStatement codeExpressionStatement = new CodeExpressionStatement();
            CodeExpressionStatement currentStatement = this._currentStatement;
            this._currentStatement = codeExpressionStatement;
            this.Visit(node.Expression);
            this._currentBlock.Add(codeExpressionStatement);
            this._currentStatement = currentStatement;
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            throw new NotImplementedException();
        }

        private bool HandleConstructorInvocation(MethodInvocationExpression node)
        {
            int result;
            if (node.Target.Entity.EntityType != EntityType.Constructor)
            {
                result = 0;
            }
            else
            {
                this._currentStatement.Expression = new CodeObjectCreateExpression(BooCodeDomConverter.GetType(node.Target.ToCodeString()), node.Arguments.Select(this.VisitExpr).ToArray());
                IParameter[] parameters = ((IEntityWithParameters)node.Target.Entity).GetParameters();
                int num = 0;
                int length = parameters.Length;
                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException("max");
                }
                while (num < length)
                {
                    int index = num;
                    num++;
                    IParameter[] array = parameters;
                    if (string.Compare(array[RuntimeServices.NormalizeArrayIndex(array, index)].Type.Name, "single", true) == 0)
                    {
                        CodeExpression codeExpression = ((CodeObjectCreateExpression)this._currentStatement.Expression).Parameters[index];
                        if (codeExpression is CodePrimitiveExpression)
                        {
                            ((CodeObjectCreateExpression)this._currentStatement.Expression).Parameters[index] = new CodeCastExpression(BooCodeDomConverter.GetType("single"), codeExpression);
                        }
                    }
                }
                result = 1;
            }
            return (byte)result != 0;
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (!this.HandleConstructorInvocation(node))
            {
                CodeMethodReferenceExpression codeMethodReferenceExpression = new CodeMethodReferenceExpression();
                if (node.Target is SuperLiteralExpression)
                {
                    codeMethodReferenceExpression.TargetObject = new CodeBaseReferenceExpression();
                }
                else
                {
                    ReferenceExpression referenceExpression = (ReferenceExpression)node.Target;
                    codeMethodReferenceExpression.MethodName = referenceExpression.Name;
                    MemberReferenceExpression memberReferenceExpression = node.Target as MemberReferenceExpression;
                    if (memberReferenceExpression != null)
                    {
                        codeMethodReferenceExpression.TargetObject = this.VisitExpr(memberReferenceExpression.Target);
                    }
                    else
                    {
                        codeMethodReferenceExpression.TargetObject = new CodeThisReferenceExpression();
                    }
                }
                CodeMethodInvokeExpression expression = new CodeMethodInvokeExpression(codeMethodReferenceExpression, node.Arguments.Select(this.VisitExpr).ToArray());
                this._currentStatement.Expression = expression;
            }
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            throw new NotImplementedException();
        }

        private bool ConvertBinaryOperator(BinaryOperatorType op, ref CodeBinaryOperatorType result)
        {
            int result2;
            if (BooCodeDomConverter.OPS.ContainsKey(op))
            {
                result = (CodeBinaryOperatorType)((Hashtable)BooCodeDomConverter.OPS)[op];
                result2 = 1;
            }
            else
            {
                result2 = 0;
            }
            return (byte)result2 != 0;
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            CodeBinaryOperatorType op = default(CodeBinaryOperatorType);
            if (!this.ConvertBinaryOperator(node.Operator, ref op))
            {
                this._currentStatement.Expression = new CodeSnippetExpression(node.ToCodeString());
            }
            else
            {
                CodeBinaryOperatorExpression expression = new CodeBinaryOperatorExpression(this.VisitExpr(node.Left), op, this.VisitExpr(node.Right));
                this._currentStatement.Expression = expression;
            }
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            this._currentStatement.Expression = new CodeArgumentReferenceExpression(node.Name);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (node.Entity is IField)
            {
                this._currentStatement.Expression = new CodeFieldReferenceExpression(this.VisitExpr(node.Target), node.Name);
                return;
            }
            if (node.Entity is IProperty)
            {
                this._currentStatement.Expression = new CodePropertyReferenceExpression(this.VisitExpr(node.Target), node.Name);
                return;
            }
            if (node.Entity is IType)
            {
                this._currentStatement.Expression = new CodeTypeReferenceExpression(BooCodeDomConverter.GetType(node.ToCodeString()));
                return;
            }
            if (node.Entity is IEvent)
            {
                this._currentStatement.Expression = new CodeEventReferenceExpression(this.VisitExpr(node.Target), node.Name);
                return;
            }
            if (node.Entity is IMethod)
            {
                this._currentStatement.Expression = new CodeMethodReferenceExpression(this.VisitExpr(node.Target), node.Name);
                return;
            }
            throw new Exception(new StringBuilder("Unknown member type for ").Append(node.ToCodeString()).ToString());
        }

        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnQuasiquoteExpression(QuasiquoteExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnStringLiteralExpression(StringLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(node.Value);
        }

        public override void OnCharLiteralExpression(CharLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(node.Value);
        }

        public override void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnIntegerLiteralExpression(IntegerLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(checked((int)node.Value));
        }

        public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(node.Value);
        }

        public override void OnNullLiteralExpression(NullLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(null);
        }

        public override void OnSelfLiteralExpression(SelfLiteralExpression node)
        {
            this._currentStatement.Expression = new CodeThisReferenceExpression();
        }

        public override void OnSuperLiteralExpression(SuperLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnBoolLiteralExpression(BoolLiteralExpression node)
        {
            this._currentStatement.Expression = new CodePrimitiveExpression(node.Value);
        }

        public override void OnRELiteralExpression(RELiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceExpression(SpliceExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceTypeReference(SpliceTypeReference node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceMemberReferenceExpression(SpliceMemberReferenceExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceTypeMember(SpliceTypeMember node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceTypeDefinitionBody(SpliceTypeDefinitionBody node)
        {
            throw new NotImplementedException();
        }

        public override void OnSpliceParameterDeclaration(SpliceParameterDeclaration node)
        {
            throw new NotImplementedException();
        }

        public override void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnHashLiteralExpression(HashLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnListLiteralExpression(ListLiteralExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnCollectionInitializationExpression(CollectionInitializationExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            this._currentStatement.Expression = new CodeArrayCreateExpression(BooCodeDomConverter.GetType(node.Type.ElementType.ToCodeString()), node.Items.Select(this.VisitExpr).ToArray());
        }

        public override void OnGeneratorExpression(GeneratorExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnExtendedGeneratorExpression(ExtendedGeneratorExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnSlice(Slice node)
        {
            throw new NotImplementedException();
        }

        public override void OnSlicingExpression(SlicingExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            CodeExpression codeExpression = this.VisitExpr(node.Target);
            CodeTypeReference type = BooCodeDomConverter.GetType(node.Type.ToCodeString());
            CodeMethodReferenceExpression codeMethodReferenceExpression = codeExpression as CodeMethodReferenceExpression;
            if (codeMethodReferenceExpression != null)
            {
                this._currentStatement.Expression = new CodeDelegateCreateExpression(type, codeMethodReferenceExpression.TargetObject, codeMethodReferenceExpression.MethodName);
            }
            else
            {
                this._currentStatement.Expression = new CodeCastExpression(type, codeExpression);
            }
        }

        public override void OnCastExpression(CastExpression node)
        {
            this._currentStatement.Expression = new CodeCastExpression(BooCodeDomConverter.GetType(node.Type.ToCodeString()), this.VisitExpr(node.Target));
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            throw new NotImplementedException();
        }

        public override void OnCustomStatement(CustomStatement node)
        {
            throw new NotImplementedException();
        }

        public override void OnCustomExpression(CustomExpression node)
        {
            throw new NotImplementedException();
        }

        internal CodeAttributeArgument VisitAttributesClosure3(Expression a)
        {
            return new CodeAttributeArgument(this.VisitExpr(a));
        }

        internal CodeAttributeArgument VisitAttributesClosure4(ExpressionPair na)
        {
            return new CodeAttributeArgument(na.First.ToString(), this.VisitExpr(na.Second));
        }
    }

}
