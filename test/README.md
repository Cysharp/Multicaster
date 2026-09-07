# Running tests

Use the .NET 10 SDK. The regular test projects target .NET 8; the NativeAOT test project targets .NET 10. Redis and NATS integration tests also require Docker.

```sh
dotnet test -c Release
```

The projects use xUnit.net v3 package version 4.0.0 with Microsoft.Testing.Platform (MTP). The repository global.json selects the .NET 10 SDK and MTP for dotnet test. Test executables also use the MTP command-line interface. CI uses dotnet test directly because the pinned dotnet-retest version does not support this mode; automatic retries are no longer enabled.

To run one project or select a test class:

```sh
dotnet test --project test/Multicaster.Tests -c Release
dotnet test --project test/Multicaster.Tests -c Release --filter-class Multicaster.Tests.GeneratedProxyTest
```

The core, Redis, and NATS test projects use coverlet.MTP for optional coverage:

```sh
dotnet test --project test/Multicaster.Tests -c Release --coverlet --results-directory ./artifacts/coverage
```

## NativeAOT tests

`Multicaster.NativeAotTest` uses `xunit.v3.aot` and tests generated in-memory and remote proxies, including filtering, inherited methods, dynamic arguments, and typed client results. Running it with `dotnet test` verifies managed execution; publish and run the executable to verify NativeAOT:

```sh
dotnet publish test/Multicaster.NativeAotTest -c Release -r linux-x64 -o ./artifacts/nativeaot -p:IlcTreatWarningsAsErrors=true
./artifacts/nativeaot/Multicaster.NativeAotTest
```

Choose the RID for the host (for example, `osx-arm64` on Apple Silicon or `win-x64` on Windows, where the executable has an `.exe` extension). Install the [NativeAOT build prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/). CI publishes and executes the Linux binary with AOT warnings treated as errors.

See the [xUnit.net NativeAOT documentation](https://xunit.net/docs/getting-started/v3/native-aot) for supported features and differences from reflection-based tests.
