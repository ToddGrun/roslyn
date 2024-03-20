// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteWorkspace
    {
        /// <summary>
        /// Create solution for given checksum from base solution
        /// </summary>
        private readonly struct SolutionCreator(HostServices hostServices, AssetProvider assetService, Solution baseSolution)
        {
#pragma warning disable IDE0052 // used only in DEBUG builds
            private readonly HostServices _hostServices = hostServices;
#pragma warning restore

            private readonly AssetProvider _assetProvider = assetService;
            private readonly Solution _baseSolution = baseSolution;

            public async Task<bool> IsIncrementalUpdateAsync(Checksum newSolutionChecksum, CancellationToken cancellationToken)
            {
                var newSolutionCompilationChecksums = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                    assetHint: AssetHint.None, newSolutionChecksum, cancellationToken).ConfigureAwait(false);
                var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                    assetHint: AssetHint.None, newSolutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

                var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                    assetHint: AssetHint.None, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

                // if either solution id or file path changed, then we consider it as new solution
                return _baseSolution.Id == newSolutionInfo.Id && _baseSolution.FilePath == newSolutionInfo.FilePath;
            }

            public async Task<Solution> CreateSolutionAsync(Checksum newSolutionChecksum, CancellationToken cancellationToken)
            {
                try
                {
                    var solution = _baseSolution;

                    // If we previously froze a source generated document and then held onto that, unfreeze it now. We'll re-freeze the new document
                    // if needed again later.
                    solution = solution.WithoutFrozenSourceGeneratedDocuments();

                    var oldSolutionCompilationChecksums = await solution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    var newSolutionCompilationChecksums = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                        assetHint: AssetHint.None, newSolutionChecksum, cancellationToken).ConfigureAwait(false);
                    var oldSolutionChecksums = await solution.CompilationState.SolutionState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                        assetHint: AssetHint.None, newSolutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

                    if (oldSolutionChecksums.Attributes != newSolutionChecksums.Attributes)
                    {
                        var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                            assetHint: AssetHint.None, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

                        // if either id or file path has changed, then this is not update
                        Contract.ThrowIfFalse(solution.Id == newSolutionInfo.Id && solution.FilePath == newSolutionInfo.FilePath);
                    }

                    if (oldSolutionChecksums.Projects.Checksum != newSolutionChecksums.Projects.Checksum)
                    {
                        solution = await UpdateProjectsAsync(
                            solution, oldSolutionChecksums, newSolutionChecksums, cancellationToken).ConfigureAwait(false);
                    }

                    if (oldSolutionChecksums.AnalyzerReferences.Checksum != newSolutionChecksums.AnalyzerReferences.Checksum)
                    {
                        solution = solution.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                            assetHint: AssetHint.None, newSolutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                    }

                    if (newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.HasValue &&
                        newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.HasValue)
                    {
                        var count = newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.Value.Count;
                        var _ = ArrayBuilder<(SourceGeneratedDocumentIdentity, SourceText)>.GetInstance(count, out var frozenDocuments);

                        for (var i = 0; i < count; i++)
                        {
                            var identity = await _assetProvider.GetAssetAsync<SourceGeneratedDocumentIdentity>(
                                assetHint: AssetHint.None, newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.Value[i], cancellationToken).ConfigureAwait(false);

                            var documentStateChecksums = await _assetProvider.GetAssetAsync<DocumentStateChecksums>(
                                assetHint: AssetHint.None, newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.Value.Checksums[i], cancellationToken).ConfigureAwait(false);

                            var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(assetHint: newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.Value.Ids[i], documentStateChecksums.Text, cancellationToken).ConfigureAwait(false);

                            var text = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            frozenDocuments.Add((identity, text));
                        }

                        solution = solution.WithFrozenSourceGeneratedDocuments(frozenDocuments.ToImmutable());
                    }

#if DEBUG
                    // make sure created solution has same checksum as given one
                    await ValidateChecksumAsync(newSolutionChecksum, solution, newSolutionChecksums.ProjectConeId, cancellationToken).ConfigureAwait(false);
#endif

                    return solution;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private async Task<Solution> UpdateProjectsAsync(
                Solution solution, SolutionStateChecksums oldSolutionChecksums, SolutionStateChecksums newSolutionChecksums, CancellationToken cancellationToken)
            {
                var solutionState = solution.SolutionState;

                using var _1 = PooledDictionary<ProjectId, Checksum>.GetInstance(out var oldProjectIdToChecksum);
                using var _2 = PooledDictionary<ProjectId, Checksum>.GetInstance(out var newProjectIdToChecksum);
                using var _3 = PooledHashSet<ProjectId>.GetInstance(out var allProjectIds);

                foreach (var (oldChecksum, projectId) in oldSolutionChecksums.Projects)
                    oldProjectIdToChecksum.Add(projectId, oldChecksum);

                foreach (var (newChecksum, projectId) in newSolutionChecksums.Projects)
                    newProjectIdToChecksum.Add(projectId, newChecksum);

                allProjectIds.AddRange(oldSolutionChecksums.Projects.Ids);
                allProjectIds.AddRange(newSolutionChecksums.Projects.Ids);

                // If there are old projects that are now missing on the new side, and this is a projectConeSync, then
                // exclude them from the old side as well.  This way we only consider projects actually added or
                // changed.
                //
                // Importantly, this means in the event of a cone-sync, we never drop projects locally.  That's very
                // desirable as it will likely be useful in future calls to still know about that project info without
                // it being dropped and having to be resynced.
                var isConeSync = newSolutionChecksums.ProjectConeId != null;
                if (isConeSync)
                {
                    foreach (var (oldChecksum, oldProjectId) in oldSolutionChecksums.Projects)
                    {
                        if (!newProjectIdToChecksum.ContainsKey(oldProjectId))
                            oldProjectIdToChecksum.Remove(oldProjectId);
                    }

                    // All the old projects must be in the new project set.  Though the reverse doesn't have to hold.
                    // The new project set may contain additional projects to add.
                    Contract.ThrowIfFalse(oldProjectIdToChecksum.Keys.All(newProjectIdToChecksum.Keys.Contains));
                }

                // remove projects that are the same on both sides.
                foreach (var projectId in allProjectIds)
                {
                    if (oldProjectIdToChecksum.TryGetValue(projectId, out var oldChecksum) &&
                        newProjectIdToChecksum.TryGetValue(projectId, out var newChecksum) &&
                        oldChecksum == newChecksum)
                    {
                        oldProjectIdToChecksum.Remove(projectId);
                        newProjectIdToChecksum.Remove(projectId);
                    }
                }

                using var _4 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var oldProjectIdToStateChecksums);
                using var _5 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var newProjectIdToStateChecksums);

                foreach (var (projectId, oldChecksum) in oldProjectIdToChecksum)
                {
                    var oldProjectState = solutionState.GetRequiredProjectState(projectId);

                    // this should be cheap since we already computed oldSolutionChecksums (which calls into this).
                    var oldProjectStateChecksums = await oldProjectState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfTrue(oldProjectStateChecksums.ProjectId != projectId);
                    Contract.ThrowIfTrue(oldChecksum != oldProjectStateChecksums.Checksum);

                    oldProjectIdToStateChecksums.Add(projectId, oldProjectStateChecksums);
                }

                // sync over the *info* about all the added/changed projects.  We'll want the info so we can determine
                // what actually changed.
                using var _6 = PooledHashSet<Checksum>.GetInstance(out var newChecksumsToSync);
                newChecksumsToSync.AddRange(newProjectIdToChecksum.Values);

                var newProjectStateChecksums = await _assetProvider.GetAssetsAsync<ProjectStateChecksums>(
                    assetHint: AssetHint.None, newChecksumsToSync, cancellationToken).ConfigureAwait(false);

                foreach (var (checksum, newProjectStateChecksum) in newProjectStateChecksums)
                {
                    Contract.ThrowIfTrue(checksum != newProjectStateChecksum.Checksum);
                    newProjectIdToStateChecksums.Add(newProjectStateChecksum.ProjectId, newProjectStateChecksum);
                }

                // Now that we've collected the old and new project state checksums, we can actually process them to
                // determine what to remove, what to add, and what to change.
                solution = await UpdateProjectsAsync(
                    solution, isConeSync, oldProjectIdToStateChecksums, newProjectIdToStateChecksums, cancellationToken).ConfigureAwait(false);

                return solution;
            }

            private async Task<Solution> UpdateProjectsAsync(
                Solution solution,
                bool isConeSync,
                Dictionary<ProjectId, ProjectStateChecksums> oldProjectIdToStateChecksums,
                Dictionary<ProjectId, ProjectStateChecksums> newProjectIdToStateChecksums,
                CancellationToken cancellationToken)
            {
                var addProjectsBuilder = ArrayBuilder<ProjectInfo>.GetInstance(newProjectIdToStateChecksums.Count);
                var withProjectReferencesBuilder = ArrayBuilder<(ProjectId, ImmutableArray<ProjectReference>)>.GetInstance(newProjectIdToStateChecksums.Count);
                var removeProjectBuilder = ArrayBuilder<ProjectId>.GetInstance(newProjectIdToStateChecksums.Count);
                var stringBuilder = new StringBuilder();

                // added project
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                {
                    if (!oldProjectIdToStateChecksums.ContainsKey(projectId))
                    {
                        // bulk sync added project assets fully since we'll definitely need that data, and we won't want
                        // to make tons of intermediary calls for it.

                        await _assetProvider.SynchronizeProjectAssetsAsync(newProjectChecksums, cancellationToken).ConfigureAwait(false);
                        var projectInfo = await _assetProvider.CreateProjectInfoAsync(projectId, newProjectChecksums.Checksum, cancellationToken).ConfigureAwait(false);

                        addProjectsBuilder.Add(projectInfo);
                    }
                }

                stringBuilder.Append("add: " + addProjectsBuilder.Count);
                solution = solution.AddProjects(addProjectsBuilder.ToImmutableAndFree());

                // remove all project references from projects that changed. this ensures exceptions will not occur for
                // cyclic references during an incremental update.
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                {
                    // Only have to do something if this was a changed project, and specifically the project references
                    // changed.
                    if (oldProjectIdToStateChecksums.TryGetValue(projectId, out var oldProjectChecksums) &&
                        oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                    {
                        withProjectReferencesBuilder.Add((projectId, ImmutableArray<ProjectReference>.Empty));
                    }
                }

                stringBuilder.Append(", refs: " + withProjectReferencesBuilder.Count);
                solution = solution.WithProjectReferences(withProjectReferencesBuilder.ToImmutableAndFree());

                // removed project
                foreach (var (projectId, _) in oldProjectIdToStateChecksums)
                {
                    if (!newProjectIdToStateChecksums.ContainsKey(projectId))
                    {
                        // Should never be removing projects during cone syncing.
                        Contract.ThrowIfTrue(isConeSync);
                        removeProjectBuilder.Add(projectId);
                    }
                }

                stringBuilder.Append(", remove: " + removeProjectBuilder.Count);
                solution = solution.RemoveProjects(removeProjectBuilder.ToImmutableAndFree());

                var updateCount = 0;
                var documentUpdateBuilder = ArrayBuilder<(ProjectId projectId, ProjectStateChecksums oldProjectChecksums, ProjectStateChecksums newProjectChecksums)>.GetInstance(newProjectIdToStateChecksums.Count);
                // changed project
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                {
                    if (oldProjectIdToStateChecksums.TryGetValue(projectId, out var oldProjectChecksums))
                    {
                        // If this project was in the old map, then the project must have changed.  Otherwise, we would
                        // have removed it earlier on.
                        Contract.ThrowIfTrue(oldProjectChecksums.Checksum == newProjectChecksums.Checksum);
                        solution = await UpdateProjectAsync(
                            solution.GetRequiredProject(projectId), oldProjectChecksums, newProjectChecksums, cancellationToken).ConfigureAwait(false);

                        documentUpdateBuilder.Add((projectId, oldProjectChecksums, newProjectChecksums));
                        updateCount++;
                    }
                }

                stringBuilder.Append(", update: " + updateCount);
                solution = await UpdateProjectDocumentsAsync(solution, documentUpdateBuilder.ToImmutableAndFree(), cancellationToken).ConfigureAwait(false);

                s_updateProjectDebugInfo.Add(stringBuilder.ToString());
                return solution;
            }

            private static readonly List<string> s_updateProjectDebugInfo = [];
            private static int s_UpdateAsyncCallCount = 0;
            private static int s_projectReferencesChanged = 0;
            private static int s_analyzerReferencesChanged = 0;
            private static int s_docsChanged = 0;
            private static int s_additionalDocsChanged = 0;
            private static int s_analyzerConfigDocsChanged = 0;

            private async Task<Solution> UpdateProjectDocumentsAsync(
                Solution solution,
                ImmutableArray<(ProjectId project, ProjectStateChecksums oldProjectChecksums,
                ProjectStateChecksums newProjectChecksums)> projects,
                CancellationToken cancellationToken)
            {
                var updateArgsDocumentsBuilder = ArrayBuilder<UpdateDocumentsAsyncArgs<DocumentState>>.GetInstance(projects.Length);
                var updateArgsAdditionalDocumentsBuilder = ArrayBuilder<UpdateDocumentsAsyncArgs<AdditionalDocumentState>>.GetInstance(projects.Length);
                var updateArgsAnalyzerConfigDocumentsBuilder = ArrayBuilder<UpdateDocumentsAsyncArgs<AnalyzerConfigDocumentState>>.GetInstance(projects.Length);

                foreach (var (projectId, oldProjectChecksums, newProjectChecksums) in projects)
                {
                    var project = solution.GetRequiredProject(projectId);

                    // changed documents
                    if (oldProjectChecksums.Documents.Checksum != newProjectChecksums.Documents.Checksum)
                    {
                        Interlocked.Increment(ref s_docsChanged);

                        var arg = new UpdateDocumentsAsyncArgs<DocumentState>(projectId, newProjectChecksums, project.State.DocumentStates, oldProjectChecksums.Documents, newProjectChecksums.Documents);
                        updateArgsDocumentsBuilder.Add(arg);
                    }

                    // changed additional documents
                    if (oldProjectChecksums.AdditionalDocuments.Checksum != newProjectChecksums.AdditionalDocuments.Checksum)
                    {
                        Interlocked.Increment(ref s_additionalDocsChanged);

                        var arg = new UpdateDocumentsAsyncArgs<AdditionalDocumentState>(projectId, newProjectChecksums, project.State.AdditionalDocumentStates, oldProjectChecksums.AdditionalDocuments, newProjectChecksums.AdditionalDocuments);
                        updateArgsAdditionalDocumentsBuilder.Add(arg);
                    }

                    // changed analyzer config documents
                    if (oldProjectChecksums.AnalyzerConfigDocuments.Checksum != newProjectChecksums.AnalyzerConfigDocuments.Checksum)
                    {
                        Interlocked.Increment(ref s_analyzerConfigDocsChanged);

                        var arg = new UpdateDocumentsAsyncArgs<AnalyzerConfigDocumentState>(projectId, newProjectChecksums, project.State.AnalyzerConfigDocumentStates, oldProjectChecksums.AnalyzerConfigDocuments, newProjectChecksums.AnalyzerConfigDocuments);
                        updateArgsAnalyzerConfigDocumentsBuilder.Add(arg);
                    }
                }

                if (updateArgsDocumentsBuilder.Count > 0)
                {
                    solution = await UpdateDocumentsAsync(
                        solution,
                        updateArgsDocumentsBuilder,
                        static (solution, documents) => solution.AddDocuments(documents),
                        static (solution, documentIds) => solution.RemoveDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }
                updateArgsDocumentsBuilder.Free();

                if (updateArgsAdditionalDocumentsBuilder.Count > 0)
                {
                    solution = await UpdateDocumentsAsync(
                        solution,
                        updateArgsAdditionalDocumentsBuilder,
                        static (solution, documents) => solution.AddAdditionalDocuments(documents),
                        static (solution, documentIds) => solution.RemoveAdditionalDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }
                updateArgsAdditionalDocumentsBuilder.Free();

                if (updateArgsAnalyzerConfigDocumentsBuilder.Count > 0)
                {
                    solution = await UpdateDocumentsAsync(
                        solution,
                        updateArgsAnalyzerConfigDocumentsBuilder,
                        static (solution, documents) => solution.AddAnalyzerConfigDocuments(documents),
                        static (solution, documentIds) => solution.RemoveAnalyzerConfigDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }
                updateArgsAnalyzerConfigDocumentsBuilder.Free();

                return solution;
            }

            private async Task<Solution> UpdateProjectAsync(Project project, ProjectStateChecksums oldProjectChecksums, ProjectStateChecksums newProjectChecksums, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref s_UpdateAsyncCallCount);

                // changed info
                if (oldProjectChecksums.Info != newProjectChecksums.Info)
                {
                    project = await UpdateProjectInfoAsync(project, newProjectChecksums.Info, cancellationToken).ConfigureAwait(false);
                }

                // changed compilation options
                if (oldProjectChecksums.CompilationOptions != newProjectChecksums.CompilationOptions)
                {
                    project = project.WithCompilationOptions(
                        project.State.ProjectInfo.Attributes.FixUpCompilationOptions(
                            await _assetProvider.GetAssetAsync<CompilationOptions>(
                                assetHint: project.Id, newProjectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false)));
                }

                // changed parse options
                if (oldProjectChecksums.ParseOptions != newProjectChecksums.ParseOptions)
                {
                    project = project.WithParseOptions(await _assetProvider.GetAssetAsync<ParseOptions>(
                        assetHint: project.Id, newProjectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false));
                }

                // changed project references
                if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                {
                    Interlocked.Increment(ref s_projectReferencesChanged);

                    project = project.WithProjectReferences(await _assetProvider.CreateCollectionAsync<ProjectReference>(
                        assetHint: project.Id, newProjectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed metadata references
                if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
                {
                    project = project.WithMetadataReferences(await _assetProvider.CreateCollectionAsync<MetadataReference>(
                        assetHint: project.Id, newProjectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
                {
                    Interlocked.Increment(ref s_analyzerReferencesChanged);

                    project = project.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                        assetHint: project.Id, newProjectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                }

                return project.Solution;
            }

            private async Task<Project> UpdateProjectInfoAsync(Project project, Checksum infoChecksum, CancellationToken cancellationToken)
            {
                var newProjectAttributes = await _assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(
                    assetHint: project.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

                // there is no API to change these once project is created
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Id == newProjectAttributes.Id);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Language == newProjectAttributes.Language);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.IsSubmission == newProjectAttributes.IsSubmission);

                var projectId = project.Id;

                if (project.State.ProjectInfo.Attributes.Name != newProjectAttributes.Name)
                {
                    project = project.Solution.WithProjectName(projectId, newProjectAttributes.Name).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.AssemblyName != newProjectAttributes.AssemblyName)
                {
                    project = project.Solution.WithProjectAssemblyName(projectId, newProjectAttributes.AssemblyName).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.FilePath != newProjectAttributes.FilePath)
                {
                    project = project.Solution.WithProjectFilePath(projectId, newProjectAttributes.FilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.OutputFilePath != newProjectAttributes.OutputFilePath)
                {
                    project = project.Solution.WithProjectOutputFilePath(projectId, newProjectAttributes.OutputFilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.OutputRefFilePath != newProjectAttributes.OutputRefFilePath)
                {
                    project = project.Solution.WithProjectOutputRefFilePath(projectId, newProjectAttributes.OutputRefFilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.CompilationOutputInfo != newProjectAttributes.CompilationOutputInfo)
                {
                    project = project.Solution.WithProjectCompilationOutputInfo(project.Id, newProjectAttributes.CompilationOutputInfo).GetProject(project.Id)!;
                }

                if (project.State.ProjectInfo.Attributes.DefaultNamespace != newProjectAttributes.DefaultNamespace)
                {
                    project = project.Solution.WithProjectDefaultNamespace(projectId, newProjectAttributes.DefaultNamespace).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.HasAllInformation != newProjectAttributes.HasAllInformation)
                {
                    project = project.Solution.WithHasAllInformation(projectId, newProjectAttributes.HasAllInformation).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.RunAnalyzers != newProjectAttributes.RunAnalyzers)
                {
                    project = project.Solution.WithRunAnalyzers(projectId, newProjectAttributes.RunAnalyzers).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.ChecksumAlgorithm != newProjectAttributes.ChecksumAlgorithm)
                {
                    project = project.Solution.WithProjectChecksumAlgorithm(projectId, newProjectAttributes.ChecksumAlgorithm).GetProject(projectId)!;
                }

                return project;
            }

            private readonly record struct UpdateDocumentsAsyncArgs<TDocumentState>(
                ProjectId ProjectId,
                ProjectStateChecksums ProjectChecksums,
                TextDocumentStates<TDocumentState> ExistingTextDocumentStates,
                ChecksumsAndIds<DocumentId> OldChecksums,
                ChecksumsAndIds<DocumentId> NewChecksums) where TDocumentState : TextDocumentState;

            private async Task<Solution> UpdateDocumentsAsync<TDocumentState>(
                Solution solution,
                ArrayBuilder<UpdateDocumentsAsyncArgs<TDocumentState>> argsBuilder,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
                CancellationToken cancellationToken) where TDocumentState : TextDocumentState
            {
                using var _1 = PooledHashSet<Checksum>.GetInstance(out var olds);
                using var _2 = PooledHashSet<Checksum>.GetInstance(out var news);

                foreach (var arg in argsBuilder)
                {
                    olds.AddRange(arg.OldChecksums.Checksums.Children);
                    news.AddRange(arg.NewChecksums.Checksums.Children);

                    // remove documents that exist in both side
                    olds.ExceptWith(arg.NewChecksums.Checksums);
                    news.ExceptWith(arg.OldChecksums.Checksums);
                }

                using var _3 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var oldDocumentIdToStateChecksums);
                using var _4 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var newDocumentIdToStateChecksums);

                await populateOldDocumentMapAsync(argsBuilder, olds, oldDocumentIdToStateChecksums, cancellationToken).ConfigureAwait(false);
                await populateNewDocumentMapAsync(this, argsBuilder, news, newDocumentIdToStateChecksums, cancellationToken).ConfigureAwait(false);

                // If more than two documents changed during a single update, perform a bulk synchronization on the
                // project to avoid large numbers of small synchronization calls during document updates.
                // 🔗 https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1365014
                if (newDocumentIdToStateChecksums.Count > 2)
                {
                    foreach (var arg in argsBuilder)
                    {
                        await _assetProvider.SynchronizeProjectAssetsAsync(arg.ProjectChecksums, cancellationToken).ConfigureAwait(false);
                    }
                }

                // added document
                ImmutableArray<DocumentInfo>.Builder? lazyDocumentsToAdd = null;
                foreach (var (documentId, newDocumentChecksums) in newDocumentIdToStateChecksums)
                {
                    if (!oldDocumentIdToStateChecksums.ContainsKey(documentId))
                    {
                        lazyDocumentsToAdd ??= ImmutableArray.CreateBuilder<DocumentInfo>();

                        // we have new document added
                        var documentInfo = await _assetProvider.CreateDocumentInfoAsync(
                            documentId, newDocumentChecksums.Checksum, cancellationToken).ConfigureAwait(false);
                        lazyDocumentsToAdd.Add(documentInfo);
                    }
                }

                if (lazyDocumentsToAdd != null)
                {
                    solution = addDocuments(solution, lazyDocumentsToAdd.ToImmutable());
                }

                var updatedDocInfo = ArrayBuilder<(DocumentId documentId, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums)>.GetInstance();

                // changed document
                foreach (var (documentId, newDocumentChecksums) in newDocumentIdToStateChecksums)
                {
                    if (!oldDocumentIdToStateChecksums.TryGetValue(documentId, out var oldDocumentChecksums))
                    {
                        continue;
                    }

                    Contract.ThrowIfTrue(oldDocumentChecksums.Checksum == newDocumentChecksums.Checksum);

                    updatedDocInfo.Add((documentId, oldDocumentChecksums, newDocumentChecksums));
                }

                solution = await UpdateDocumentsAsync(solution, updatedDocInfo, cancellationToken).ConfigureAwait(false);

                // removed document
                ImmutableArray<DocumentId>.Builder? lazyDocumentsToRemove = null;
                foreach (var (documentId, _) in oldDocumentIdToStateChecksums)
                {
                    if (!newDocumentIdToStateChecksums.ContainsKey(documentId))
                    {
                        // we have a document removed
                        lazyDocumentsToRemove ??= ImmutableArray.CreateBuilder<DocumentId>();
                        lazyDocumentsToRemove.Add(documentId);
                    }
                }

                if (lazyDocumentsToRemove is not null)
                {
                    solution = removeDocuments(solution, lazyDocumentsToRemove.ToImmutable());
                }

                return solution;

                static async Task populateOldDocumentMapAsync(
                    ArrayBuilder<UpdateDocumentsAsyncArgs<TDocumentState>> argsBuilder,
                    PooledHashSet<Checksum> olds,
                    PooledDictionary<DocumentId, DocumentStateChecksums> oldDocumentIdToStateChecksums,
                    CancellationToken cancellationToken)
                {
                    foreach (var arg in argsBuilder)
                    {
                        foreach (var (_, state) in arg.ExistingTextDocumentStates.States)
                        {
                            var documentChecksums = await state.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                            if (olds.Contains(documentChecksums.Checksum))
                                oldDocumentIdToStateChecksums.Add(state.Id, documentChecksums);
                        }
                    }
                }

                static async Task populateNewDocumentMapAsync(
                    SolutionCreator @this,
                    ArrayBuilder<UpdateDocumentsAsyncArgs<TDocumentState>> argsBuilder,
                    PooledHashSet<Checksum> news,
                    PooledDictionary<DocumentId, DocumentStateChecksums> newDocumentIdToStateChecksums,
                    CancellationToken cancellationToken)
                {
                    foreach (var arg in argsBuilder)
                    {
                        var documentStateChecksums = await @this._assetProvider.GetAssetsAsync<DocumentStateChecksums>(
                            assetHint: arg.ProjectId, news, cancellationToken).ConfigureAwait(false);

                        foreach (var (_, documentStateChecksum) in documentStateChecksums)
                            newDocumentIdToStateChecksums.Add(documentStateChecksum.DocumentId, documentStateChecksum);
                    }
                }
            }

            private async Task<Solution> UpdateDocumentsAsync(
                Solution solution,
                ArrayBuilder<(DocumentId documentId, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums)> documentInfo,
                CancellationToken cancellationToken)
            {
                var withDocumentTextBuilder = ArrayBuilder<(DocumentId, SourceText)>.GetInstance();
                var withAdditionalDocumentTextBuilder = ArrayBuilder<(DocumentId, SourceText)>.GetInstance();
                var withAnalyzerConfigDocumentTextBuilder = ArrayBuilder<(DocumentId, SourceText)>.GetInstance();

                foreach (var (documentId, oldDocumentChecksums, newDocumentChecksums) in documentInfo)
                {
                    var project = solution.GetRequiredProject(documentId.ProjectId);
                    var document = project.GetDocument(documentId) ?? project.GetAdditionalDocument(documentId) ?? project.GetAnalyzerConfigDocument(documentId);
                    Contract.ThrowIfNull(document);

                    // changed info
                    if (oldDocumentChecksums.Info != newDocumentChecksums.Info)
                    {
                        document = await UpdateDocumentInfoAsync(document, newDocumentChecksums.Info, cancellationToken).ConfigureAwait(false);
                        solution = document.Project.Solution;
                    }

                    // changed text
                    if (oldDocumentChecksums.Text != newDocumentChecksums.Text)
                    {
                        var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(
                            assetHint: document.Id, newDocumentChecksums.Text, cancellationToken).ConfigureAwait(false);
                        var sourceText = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);

                        switch (document.Kind)
                        {
                            case TextDocumentKind.Document:
                                withDocumentTextBuilder.Add((documentId, sourceText));
                                break;
                            case TextDocumentKind.AnalyzerConfigDocument:
                                withAnalyzerConfigDocumentTextBuilder.Add((documentId, sourceText));
                                break;
                            case TextDocumentKind.AdditionalDocument:
                                withAdditionalDocumentTextBuilder.Add((documentId, sourceText));
                                break;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(document.Kind);
                        }
                    }
                }

                if (withDocumentTextBuilder.Count > 0)
                    solution = solution.WithDocumentTexts(withDocumentTextBuilder.ToImmutableAndFree());
                else
                    withDocumentTextBuilder.Free();

                // TODO: other two

                return solution;
            }

            private async Task<TextDocument> UpdateDocumentInfoAsync(TextDocument document, Checksum infoChecksum, CancellationToken cancellationToken)
            {
                var newDocumentInfo = await _assetProvider.GetAssetAsync<DocumentInfo.DocumentAttributes>(
                    assetHint: document.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

                // there is no api to change these once document is created
                Contract.ThrowIfFalse(document.State.Attributes.Id == newDocumentInfo.Id);
                Contract.ThrowIfFalse(document.State.Attributes.Name == newDocumentInfo.Name);
                Contract.ThrowIfFalse(document.State.Attributes.FilePath == newDocumentInfo.FilePath);
                Contract.ThrowIfFalse(document.State.Attributes.IsGenerated == newDocumentInfo.IsGenerated);
                Contract.ThrowIfFalse(document.State.Attributes.DesignTimeOnly == newDocumentInfo.DesignTimeOnly);

                if (document.State.Attributes.Folders != newDocumentInfo.Folders)
                {
                    // additional document can't change folder once created
                    Contract.ThrowIfFalse(document is Document);
                    document = document.Project.Solution.WithDocumentFolders(document.Id, newDocumentInfo.Folders).GetDocument(document.Id)!;
                }

                if (document.State.Attributes.SourceCodeKind != newDocumentInfo.SourceCodeKind)
                {
                    // additional document can't change sourcecode kind once created
                    Contract.ThrowIfFalse(document is Document);
                    document = document.Project.Solution.WithDocumentSourceCodeKind(document.Id, newDocumentInfo.SourceCodeKind).GetDocument(document.Id)!;
                }

                return document;
            }

#if DEBUG
            private async Task ValidateChecksumAsync(
                Checksum checksumFromRequest,
                Solution incrementalSolutionBuilt,
                ProjectId? projectConeId,
                CancellationToken cancellationToken)
            {
                // In the case of a cone sync, we only want to compare the checksum of the cone sync'ed over to the
                // current checksum of that same cone. What is outside of those cones is totally allowed to be
                // different.
                //
                // Note: this is acceptable because that's the contract of a cone sync.  Features themselves are not
                // allowed to cone-sync and then do anything that needs host/remote invariants outside of that cone.
                var currentSolutionChecksum = projectConeId == null
                    ? await incrementalSolutionBuilt.CompilationState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
                    : await incrementalSolutionBuilt.CompilationState.GetChecksumAsync(projectConeId, cancellationToken).ConfigureAwait(false);

                if (checksumFromRequest == currentSolutionChecksum)
                    return;

                var solutionInfo = await _assetProvider.CreateSolutionInfoAsync(checksumFromRequest, cancellationToken).ConfigureAwait(false);
                var workspace = new AdhocWorkspace(_hostServices);
                workspace.AddSolution(solutionInfo);

                await TestUtils.AssertChecksumsAsync(_assetProvider, checksumFromRequest, workspace.CurrentSolution, incrementalSolutionBuilt).ConfigureAwait(false);
            }
#endif
        }
    }
}
