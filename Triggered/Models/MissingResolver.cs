using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Triggered.Models
{
    /// <summary>
    /// Defines a custom <see cref="MetadataReferenceResolver"/> that does not resolve missing assembles.
    /// </summary>
    public class MissingResolver : MetadataReferenceResolver
    {
        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override bool Equals(object? other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns false, will never resolve missing assemblies.
        /// </summary>
        public override bool ResolveMissingAssemblies => false;

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }
}
