//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using Microsoft.CodeAnalysis.Host.Mef;
//using Microsoft.Internal.VisualStudio.PlatformUI;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Utilities;

//namespace Microsoft.VisualStudio.LanguageServices.Progression.V2.Providers;

//[Export(typeof(IAttachedCollectionSourceProvider))]
//[Name(HierarchyItemsProviderNames.Contains)]
//[method: ImportingConstructor]
//[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
//internal sealed class HierarchyContainsProvider() : AttachedCollectionSourceProvider<IVsHierarchyItem>
//{
//    static readonly IAttachedRelationship[] SupportedRelationships = new IAttachedRelationship[]
//    {
//        new ContainsAttachedRelationship()
//    };

//    protected override IEnumerable<IAttachedRelationship> GetRelationships(IVsHierarchyItem item)
//    {
//        return SupportedRelationships;
//    }

//    protected override IAttachedCollectionSource CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
//    {
//        if (relationshipName == KnownRelationships.Contains)
//        {
//            return new HierarchyAttachedCollectionSource(item);
//        }

//        return null;
//    }
//}
