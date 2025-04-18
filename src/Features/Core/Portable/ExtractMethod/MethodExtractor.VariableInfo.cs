﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        protected sealed class VariableInfo(
            VariableSymbol variableSymbol,
            VariableStyle variableStyle,
            bool useAsReturnValue) : IComparable<VariableInfo>
        {
            private readonly VariableSymbol _variableSymbol = variableSymbol;
            private readonly VariableStyle _variableStyle = variableStyle;
            private readonly bool _useAsReturnValue = useAsReturnValue;

            public bool UseAsReturnValue
            {
                get
                {
                    Contract.ThrowIfFalse(!_useAsReturnValue || _variableStyle.ReturnStyle.ReturnBehavior != ReturnBehavior.None);
                    return _useAsReturnValue;
                }
            }

            public bool CanBeUsedAsReturnValue
            {
                get
                {
                    return _variableStyle.ReturnStyle.ReturnBehavior != ReturnBehavior.None;
                }
            }

            public bool UseAsParameter
            {
                get
                {
                    return (!_useAsReturnValue && _variableStyle.ParameterStyle.ParameterBehavior != ParameterBehavior.None) ||
                           (_useAsReturnValue && _variableStyle.ReturnStyle.ParameterBehavior != ParameterBehavior.None);
                }
            }

            public ParameterBehavior ParameterModifier
            {
                get
                {
                    return _useAsReturnValue ? _variableStyle.ReturnStyle.ParameterBehavior : _variableStyle.ParameterStyle.ParameterBehavior;
                }
            }

            public DeclarationBehavior GetDeclarationBehavior()
                => _useAsReturnValue
                    ? _variableStyle.ReturnStyle.DeclarationBehavior
                    : _variableStyle.ParameterStyle.DeclarationBehavior;

            public ReturnBehavior ReturnBehavior
            {
                get
                {
                    if (_useAsReturnValue)
                    {
                        return _variableStyle.ReturnStyle.ReturnBehavior;
                    }

                    return ReturnBehavior.None;
                }
            }

            public static VariableInfo CreateReturnValue(VariableInfo variable)
            {
                Contract.ThrowIfNull(variable);
                Contract.ThrowIfFalse(variable.CanBeUsedAsReturnValue);
                Contract.ThrowIfFalse(variable.ParameterModifier is ParameterBehavior.Out or ParameterBehavior.Ref);

                return new VariableInfo(variable._variableSymbol, variable._variableStyle, useAsReturnValue: true);
            }

            public void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken)
            {
                _variableSymbol.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);
            }

            public string Name => _variableSymbol.Name;

            /// <summary>
            /// Returns true, if the variable could be either passed as a parameter
            /// to the new local function or the local function can capture the variable.
            /// </summary>
            public bool CanBeCapturedByLocalFunction
                => _variableSymbol.CanBeCapturedByLocalFunction;

            public bool OriginalTypeHadAnonymousTypeOrDelegate => _variableSymbol.SymbolType.ContainsAnonymousType();

            public ITypeSymbol SymbolType => _variableSymbol.SymbolType;

            public SyntaxToken GetIdentifierTokenAtDeclaration(SemanticDocument document)
                => document.GetTokenWithAnnotation(_variableSymbol.IdentifierTokenAnnotation);

            public SyntaxToken GetIdentifierTokenAtDeclaration(SyntaxNode node)
                => node.GetAnnotatedTokens(_variableSymbol.IdentifierTokenAnnotation).SingleOrDefault();

            public SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken) => _variableSymbol.GetOriginalIdentifierToken(cancellationToken);

            public int CompareTo(VariableInfo other)
                => this._variableSymbol.CompareTo(other._variableSymbol);
        }
    }
}
