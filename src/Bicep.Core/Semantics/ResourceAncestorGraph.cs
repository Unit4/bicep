// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bicep.Core.Diagnostics;
using Bicep.Core.Syntax;

namespace Bicep.Core.Semantics
{
    public sealed class ResourceAncestorGraph
    {
        private readonly ImmutableDictionary<ResourceSymbol, ImmutableArray<ResourceSymbol>> data;

        public ResourceAncestorGraph(ImmutableDictionary<ResourceSymbol, ImmutableArray<ResourceSymbol>> data)
        {
            this.data = data;
        }

        // Gets the ordered list of ancestors of this resource in order from 'oldest' to 'youngest'
        // this is the same order we need to compute the name of a resource using `/` separated segments in a string.
        public ImmutableArray<ResourceSymbol> GetAncestors(ResourceSymbol resource)
        {
            if (data.TryGetValue(resource, out var results))
            {
                return results;
            }
            else
            {
                return ImmutableArray<ResourceSymbol>.Empty;
            }
        }

        private static IEnumerable<ResourceSymbol> GetAncestorsYoungestToOldest(ImmutableDictionary<ResourceSymbol, ResourceSymbol> hierarchy, ResourceSymbol resource)
        {
            while (hierarchy.TryGetValue(resource, out var parentResource))
            {
                yield return parentResource;

                resource = parentResource;
            }
        }

        public static ResourceAncestorGraph Compute(SyntaxTree syntaxTree, IBinder binder)
        {
            var visitor = new ResourceAncestorVisitor(binder);
            visitor.Visit(syntaxTree.ProgramSyntax);

            var ancestry = visitor.Ancestry.Keys.ToImmutableDictionary(
                child => child,
                child => GetAncestorsYoungestToOldest(visitor.Ancestry, child).Reverse().ToImmutableArray());
            
            return new ResourceAncestorGraph(ancestry);
        }
    }
}