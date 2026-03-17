// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers;

[ExportCompletionProvider(nameof(KeywordCompletionProvider), LanguageNames.VisualBasic)]
[ExtensionOrder(After = nameof(FirstBuiltInCompletionProvider))]
[Shared]
internal sealed class KeywordCompletionProvider : AbstractKeywordCompletionProvider<VisualBasicSyntaxContext>
{
    private static readonly ImmutableArray<string> s_tags = ImmutableArray.Create(WellKnownTags.Intrinsic);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public KeywordCompletionProvider()
        : base(ImmutableArray.Create<IKeywordRecommender<VisualBasicSyntaxContext>>(
            new KeywordRecommenders.ArrayStatements.EraseKeywordRecommender(),
            new KeywordRecommenders.ArrayStatements.PreserveKeywordRecommender(),
            new KeywordRecommenders.ArrayStatements.ReDimKeywordRecommender(),
            new KeywordRecommenders.Declarations.AliasKeywordRecommender(),
            new KeywordRecommenders.Declarations.AsKeywordRecommender(),
            new KeywordRecommenders.Declarations.AsyncKeywordRecommender(),
            new KeywordRecommenders.Declarations.AttributeScopesKeywordRecommender(),
            new KeywordRecommenders.Declarations.CharsetModifierKeywordRecommender(),
            new KeywordRecommenders.Declarations.ClassKeywordRecommender(),
            new KeywordRecommenders.Declarations.ConstKeywordRecommender(),
            new KeywordRecommenders.Declarations.CovarianceModifiersKeywordRecommender(),
            new KeywordRecommenders.Declarations.CustomEventKeywordRecommender(),
            new KeywordRecommenders.Declarations.DeclareKeywordRecommender(),
            new KeywordRecommenders.Declarations.DelegateKeywordRecommender(),
            new KeywordRecommenders.Declarations.DelegateSubFunctionKeywordRecommender(),
            new KeywordRecommenders.Declarations.DimKeywordRecommender(),
            new KeywordRecommenders.Declarations.EndBlockKeywordRecommender(),
            new KeywordRecommenders.Declarations.EnumKeywordRecommender(),
            new KeywordRecommenders.Declarations.EventKeywordRecommender(),
            new KeywordRecommenders.Declarations.ExternalSubFunctionKeywordRecommender(),
            new KeywordRecommenders.Declarations.FunctionKeywordRecommender(),
            new KeywordRecommenders.Declarations.GenericConstraintsKeywordRecommender(),
            new KeywordRecommenders.Declarations.GetSetKeywordRecommender(),
            new KeywordRecommenders.Declarations.ImplementsKeywordRecommender(),
            new KeywordRecommenders.Declarations.ImportsKeywordRecommender(),
            new KeywordRecommenders.Declarations.InheritsKeywordRecommender(),
            new KeywordRecommenders.Declarations.InKeywordRecommender(),
            new KeywordRecommenders.Declarations.InterfaceKeywordRecommender(),
            new KeywordRecommenders.Declarations.IteratorKeywordRecommender(),
            new KeywordRecommenders.Declarations.LibKeywordRecommender(),
            new KeywordRecommenders.Declarations.ModifierKeywordsRecommender(),
            new KeywordRecommenders.Declarations.ModuleKeywordRecommender(),
            new KeywordRecommenders.Declarations.NamespaceKeywordRecommender(),
            new KeywordRecommenders.Declarations.OfKeywordRecommender(),
            new KeywordRecommenders.Declarations.OperatorKeywordRecommender(),
            new KeywordRecommenders.Declarations.OverloadableOperatorRecommender(),
            new KeywordRecommenders.Declarations.ParameterModifiersKeywordRecommender(),
            new KeywordRecommenders.Declarations.PropertyKeywordRecommender(),
            new KeywordRecommenders.Declarations.StaticKeywordRecommender(),
            new KeywordRecommenders.Declarations.StructureKeywordRecommender(),
            new KeywordRecommenders.Declarations.SubKeywordRecommender(),
            new KeywordRecommenders.Declarations.ToKeywordRecommender(),
            new KeywordRecommenders.EventHandling.AddHandlerKeywordRecommender(),
            new KeywordRecommenders.EventHandling.HandlesKeywordRecommender(),
            new KeywordRecommenders.EventHandling.RaiseEventKeywordRecommender(),
            new KeywordRecommenders.EventHandling.RemoveHandlerKeywordRecommender(),
            new KeywordRecommenders.Expressions.AddressOfKeywordRecommender(),
            new KeywordRecommenders.Expressions.BinaryOperatorKeywordRecommender(),
            new KeywordRecommenders.Expressions.CastOperatorsKeywordRecommender(),
            new KeywordRecommenders.Expressions.FromKeywordRecommender(),
            new KeywordRecommenders.Expressions.GetTypeKeywordRecommender(),
            new KeywordRecommenders.Expressions.GetXmlNamespaceKeywordRecommender(),
            new KeywordRecommenders.Expressions.GlobalKeywordRecommender(),
            new KeywordRecommenders.Expressions.IfKeywordRecommender(),
            new KeywordRecommenders.Expressions.KeyKeywordRecommender(),
            new KeywordRecommenders.Expressions.MeKeywordRecommender(),
            new KeywordRecommenders.Expressions.MyBaseKeywordRecommender(),
            new KeywordRecommenders.Expressions.MyClassKeywordRecommender(),
            new KeywordRecommenders.Expressions.NameOfKeywordRecommender(),
            new KeywordRecommenders.Expressions.NewKeywordRecommender(),
            new KeywordRecommenders.Expressions.NothingKeywordRecommender(),
            new KeywordRecommenders.Expressions.NotKeywordRecommender(),
            new KeywordRecommenders.Expressions.LambdaKeywordRecommender(),
            new KeywordRecommenders.Expressions.TrueFalseKeywordRecommender(),
            new KeywordRecommenders.Expressions.TypeOfKeywordRecommender(),
            new KeywordRecommenders.Expressions.WithKeywordRecommender(),
            new KeywordRecommenders.OnErrorStatements.ErrorKeywordRecommender(),
            new KeywordRecommenders.OnErrorStatements.GoToDestinationsRecommender(),
            new KeywordRecommenders.OnErrorStatements.GoToKeywordRecommender(),
            new KeywordRecommenders.OnErrorStatements.NextKeywordRecommender(),
            new KeywordRecommenders.OnErrorStatements.OnErrorKeywordRecommender(),
            new KeywordRecommenders.OnErrorStatements.ResumeKeywordRecommender(),
            new KeywordRecommenders.OptionStatements.CompareBinaryTextRecommender(),
            new KeywordRecommenders.OptionStatements.ExplicitOptionsRecommender(),
            new KeywordRecommenders.OptionStatements.InferOptionsRecommender(),
            new KeywordRecommenders.OptionStatements.OptionKeywordRecommender(),
            new KeywordRecommenders.OptionStatements.OptionNamesRecommender(),
            new KeywordRecommenders.OptionStatements.StrictOptionsRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.ConstDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.ElseDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.ElseIfDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.EndIfDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.EndRegionDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.IfDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.ReferenceDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.RegionDirectiveKeywordRecommender(),
            new KeywordRecommenders.PreprocessorDirectives.WarningDirectiveKeywordRecommender(),
            new KeywordRecommenders.Queries.AggregateKeywordRecommender(),
            new KeywordRecommenders.Queries.AscendingDescendingKeywordRecommender(),
            new KeywordRecommenders.Queries.DistinctKeywordRecommender(),
            new KeywordRecommenders.Queries.EqualsKeywordRecommender(),
            new KeywordRecommenders.Queries.FromKeywordRecommender(),
            new KeywordRecommenders.Queries.GroupByKeywordRecommender(),
            new KeywordRecommenders.Queries.GroupJoinKeywordRecommender(),
            new KeywordRecommenders.Queries.GroupKeywordRecommender(),
            new KeywordRecommenders.Queries.IntoKeywordRecommender(),
            new KeywordRecommenders.Queries.JoinKeywordRecommender(),
            new KeywordRecommenders.Queries.LetKeywordRecommender(),
            new KeywordRecommenders.Queries.OnKeywordRecommender(),
            new KeywordRecommenders.Queries.OrderByKeywordRecommender(),
            new KeywordRecommenders.Queries.SelectKeywordRecommender(),
            new KeywordRecommenders.Queries.SkipKeywordRecommender(),
            new KeywordRecommenders.Queries.TakeKeywordRecommender(),
            new KeywordRecommenders.Queries.WhereKeywordRecommender(),
            new KeywordRecommenders.Queries.WhileKeywordRecommender(),
            new KeywordRecommenders.Statements.CallKeywordRecommender(),
            new KeywordRecommenders.Statements.CaseKeywordRecommender(),
            new KeywordRecommenders.Statements.CatchKeywordRecommender(),
            new KeywordRecommenders.Statements.ContinueKeywordRecommender(),
            new KeywordRecommenders.Statements.DoKeywordRecommender(),
            new KeywordRecommenders.Statements.EachKeywordRecommender(),
            new KeywordRecommenders.Statements.ElseIfKeywordRecommender(),
            new KeywordRecommenders.Statements.ElseKeywordRecommender(),
            new KeywordRecommenders.Statements.EndKeywordRecommender(),
            new KeywordRecommenders.Statements.ExitKeywordRecommender(),
            new KeywordRecommenders.Statements.FinallyKeywordRecommender(),
            new KeywordRecommenders.Statements.ForKeywordRecommender(),
            new KeywordRecommenders.Statements.GotoKeywordRecommender(),
            new KeywordRecommenders.Statements.IfKeywordRecommender(),
            new KeywordRecommenders.Statements.IsKeywordRecommender(),
            new KeywordRecommenders.Statements.LoopKeywordRecommender(),
            new KeywordRecommenders.Statements.MidKeywordRecommender(),
            new KeywordRecommenders.Statements.NextKeywordRecommender(),
            new KeywordRecommenders.Statements.ReturnKeywordRecommender(),
            new KeywordRecommenders.Statements.SelectKeywordRecommender(),
            new KeywordRecommenders.Statements.StepKeywordRecommender(),
            new KeywordRecommenders.Statements.StopKeywordRecommender(),
            new KeywordRecommenders.Statements.SyncLockKeywordRecommender(),
            new KeywordRecommenders.Statements.ThenKeywordRecommender(),
            new KeywordRecommenders.Statements.ThrowKeywordRecommender(),
            new KeywordRecommenders.Statements.ToKeywordRecommender(),
            new KeywordRecommenders.Statements.TryKeywordRecommender(),
            new KeywordRecommenders.Statements.UntilAndWhileKeywordRecommender(),
            new KeywordRecommenders.Statements.UsingKeywordRecommender(),
            new KeywordRecommenders.Statements.WhenKeywordRecommender(),
            new KeywordRecommenders.Statements.WhileLoopKeywordRecommender(),
            new KeywordRecommenders.Statements.WithKeywordRecommender(),
            new KeywordRecommenders.Statements.YieldKeywordRecommender(),
            new KeywordRecommenders.Types.BuiltInTypesKeywordRecommender()))
    {
    }

    internal override string Language => LanguageNames.VisualBasic;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        // We show 'Of' after dim x as new list(
        return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options);
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharsAndParen;

    private static readonly CompletionItemRules s_tupleRules = CompletionItemRules.Default
        .WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

    protected override CompletionItem CreateItem(RecommendedKeyword keyword, VisualBasicSyntaxContext context, CancellationToken cancellationToken)
    {
        var rules = context.IsPossibleTupleContext ? s_tupleRules : CompletionItemRules.Default;

        return CommonCompletionItem.Create(
            displayText: keyword.Keyword,
            displayTextSuffix: "",
            description: keyword.DescriptionFactory(cancellationToken),
            glyph: Glyph.Keyword,
            tags: s_tags,
            rules: rules.WithMatchPriority(keyword.MatchPriority));
    }
}
