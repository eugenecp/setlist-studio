using Xunit;

namespace SetlistStudio.Tests.Collections;

/// <summary>
/// Test collection for tests that modify environment variables.
/// Tests in this collection run sequentially to prevent race conditions
/// when modifying global environment state.
/// </summary>
[CollectionDefinition("EnvironmentVariable", DisableParallelization = true)]
public class EnvironmentVariableCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}