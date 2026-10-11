using Xunit;

namespace Mythosia.Test.Integrity;

// Keep the exhaustive correctness suite predictable in its use of CPU resources.
// Cold initialization concurrency is exercised separately in isolated load contexts.
[CollectionDefinition("Integrity correctness", DisableParallelization = true)]
public sealed class IntegrityTestCollection
{
    public const string Name = "Integrity correctness";
}
