using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CliWrap;
using DataLayer.EF;
using DataLayer.EfClasses;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Playwright;
using mu88.Shared.Testing.Docker;
using mu88.Shared.Testing.Playwright;
using mu88.Shared.Testing.Testcontainers;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests.System;

[SetUpFixture]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Test fixture organization")]
public class SystemTestsFixture
{
    private const string AppNetworkAlias = "app";
    private const string AppContainerPort = "8080";
    private const string SeededDbFileName = "ShopAndEat.db";
    private const string AppDbDirectoryInContainer = "/tmp";
    private const string DockerfilePath = "testData/system/Dockerfile";

    // "/home/app/db" (the production connection string's directory, see appsettings.json) is only
    // created by the docker-compose bind mount in production - it doesn't exist in the plain app
    // image, and Testcontainers' resource mapping has no way to create an intermediate directory with
    // custom ownership (it only ever emits file-level tar entries); Docker's archive extraction then
    // auto-creates any missing parent directory as root:root 0755, which the non-root app user can't
    // write a SQLite lock/journal file into ("attempt to write a readonly database"). "/tmp" always
    // exists in the image already with world-writable (1777) permissions, sidestepping the problem
    // entirely, so the pre-seeded file is mapped there instead, with the app's own connection string
    // overridden (via "ConnectionStrings__SQLite") to match.
    //
    // WithResourceMapping(FileInfo, string, ...) copies the source file INTO the given target
    // directory, keeping the source's own file name (it does not accept a full target file path) -
    // so the host-side seeded file must itself be named exactly "ShopAndEat.db" (see
    // CreateSeededDatabaseFileAsync), or it would land under a different name/an unexpected nested
    // "directory" named after the intended file path.
    private const string AppDbPathInContainer = AppDbDirectoryInContainer + "/" + SeededDbFileName;

    // The chiseled app image runs as the non-root "app" user (APP_UID=1654 baked into the image, see
    // its Config.User/Env) - the pre-seeded DB file must be owned by that same uid/gid, or the app
    // process can't open it for read/write (WithResourceMapping's default owner is root:root).
    private const uint AppContainerUid = 1654;

    // Reference data shared by all System tests that need an ArticleGroup/Unit/MealType/Store to
    // exist - the app has no UI/API to create these (Store has no Create endpoint at all), so they
    // must be seeded directly against the app's SQLite file (see SeedReferenceDataAsync).
    public const string SeededArticleGroupName = "SystemTestGroup";
    public const string SeededUnitName = "pcs";
    public const string SeededMealTypeName = "Dinner";
    public const string SeededStoreName = "SystemTestStore";

    // Static properties exposed to tests
    public static Uri AppBaseAddress { get; private set; } = null!;
    public static Uri AppInternalAddress { get; private set; } = null!;
    public static IBrowser Browser { get; private set; } = null!;

    private CancellationTokenSource _cancellationTokenSource = null!;
    private INetwork? _network;
    private IContainer? _appContainer;
    private PlaywrightSession? _playwrightSession;

    private static async Task BuildDockerImageOfAppAsync(string containerImageTag)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var projectFile = Path.Join(rootDirectory.FullName, "ShopAndEat", "ShopAndEat.csproj");
        await DockerImageBuilder.BuildAsync(projectFile, containerImageTag, "shopandeat", rootDirectory.FullName, CancellationToken.None);
    }

    // Prepares a fully migrated + seeded SQLite DB file on the host, using the real DataLayer
    // migrations and entities (not raw SQL, not a production-code test endpoint). The file is later
    // mapped into the app container (see AppDbPathInContainer) BEFORE the container starts, via
    // Testcontainers' WithResourceMapping. This deliberately avoids mutating the SQLite file of an
    // already-running container: besides SQLite's WAL journal mode splitting data across "-wal"/"-shm"
    // sidecar files, Microsoft.Data.Sqlite's connection pooling inside the running app process would
    // keep serving stale (pre-seed) data from its already-open native handle even after the file
    // content on disk was swapped out from under it. Preparing the file up front means the app's own
    // startup migration (CreateDbIfNotExists in Program.cs) is a safe no-op against an already-up-to-
    // date schema, and every connection it ever opens sees the seeded data from the very first read.
    private static async Task<string> CreateSeededDatabaseFileAsync(CancellationToken cancellationToken)
    {
        // The host file name must match SeededDbFileName exactly - WithResourceMapping(FileInfo, ...)
        // copies into a target directory under the source's own file name, see AppDbPathInContainer.
        var tempDbDirectory = Path.Combine(Path.GetTempPath(), $"shopandeat-seed-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDbDirectory);
        var tempDbPath = Path.Combine(tempDbDirectory, SeededDbFileName);

        // ConfigureWarnings: see matching comment in ShopAndEat/Program.cs - suppresses a false-positive
        // PendingModelChangesWarning even though the checked-in model snapshot is up to date.
        var optionsBuilder = new DbContextOptionsBuilder<EfCoreContext>()
            .UseSqlite($"Data Source={tempDbPath}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        await using (var context = new EfCoreContext(optionsBuilder.Options))
        {
            await context.Database.MigrateAsync(cancellationToken);

            var articleGroup = new ArticleGroup(SeededArticleGroupName);
            context.ArticleGroups.Add(articleGroup);
            context.Units.Add(new DataLayer.EfClasses.Unit(SeededUnitName));
            context.MealTypes.Add(new MealType(SeededMealTypeName, 1));
            context.Stores.Add(new Store(SeededStoreName, [new ShoppingOrder(articleGroup, 1)]));

            await context.SaveChangesAsync(cancellationToken);

            // Consolidate everything into the single main DB file (no "-wal"/"-shm" sidecars), since
            // only this one file is mapped into the app container.
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;", cancellationToken);
        }

        // EF Core/Microsoft.Data.Sqlite pools the underlying connection by default, which would
        // otherwise keep the file handle open on the host even after the context above is disposed.
        SqliteConnection.ClearAllPools();

        return tempDbPath;
    }

    private static async Task<INetwork> CreateNetworkAsync(CancellationToken cancellationToken)
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync(cancellationToken);
        return network;
    }

    private static async Task CleanupAppImageAsync(IContainer container, CancellationToken cancellationToken)
    {
        var imageName = container.Image.FullName;

        try
        {
            await Cli.Wrap("docker")
                .WithArguments(new[] { "rmi", "--force", imageName })
                .ExecuteAsync(cancellationToken);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private string? _seededDbFilePath;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // 10 minutes to cover: Docker image publish/build, container startups, and pulling the ~1-2GB Playwright image.
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        try
        {
            var imageTag = $"0.0.0-system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}";

            // The network must exist before anything that attaches to it (app container, Playwright
            // container), but is itself cheap/fast to create - so create it first, then fan out
            // everything that can run concurrently from there: the app image build, the DB seeding,
            // and the Playwright container start (which only needs the network, NOT the app image or
            // DB - unlike the app container). The app container alone must wait for all three
            // prerequisites (network + image + seeded DB file).
            _network = await CreateNetworkAsync(_cancellationTokenSource.Token);

            await StartRemainingInfrastructureAsync(imageTag);

            var appHostPort = _appContainer!.GetMappedPublicPort(int.Parse(AppContainerPort, CultureInfo.InvariantCulture));
            AppBaseAddress = new Uri($"http://localhost:{appHostPort}/shopAndEat");
            AppInternalAddress = new Uri($"http://{AppNetworkAlias}:{AppContainerPort}/shopAndEat");

            Browser = _playwrightSession!.Browser;
        }
        catch
        {
            await Cleanup();
            throw;
        }
    }

    // Runs the app image build, the DB seeding, and the Playwright container start concurrently (all
    // three only depend on the already-created network, not on each other), then starts the app
    // container once its own prerequisites (image + seeded DB file) are ready. Field assignment
    // happens in a finally block (not after WhenAll succeeds) so that any resource created by a task
    // that DID succeed is still tracked for Cleanup(), even if a sibling task failed and WhenAll threw.
    private async Task StartRemainingInfrastructureAsync(string imageTag)
    {
        var imageBuildTask = BuildDockerImageOfAppAsync(imageTag);
        var seedDbTask = CreateSeededDatabaseFileAsync(_cancellationTokenSource.Token);
        var playwrightSessionTask = PlaywrightSession.StartAsync(
            TestcontainerImages.GetImageFromDockerfile(DockerfilePath, "playwright"),
            _network!,
            _cancellationTokenSource.Token);

        try
        {
            await Task.WhenAll(imageBuildTask, seedDbTask, playwrightSessionTask);
        }
        finally
        {
            if (seedDbTask.IsCompletedSuccessfully)
            {
                _seededDbFilePath = await seedDbTask;
            }

            if (playwrightSessionTask.IsCompletedSuccessfully)
            {
                _playwrightSession = await playwrightSessionTask;
            }
        }

        _appContainer = await StartAppContainerAsync(imageTag, _seededDbFilePath!);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await Cleanup();
    }

    private async Task<IContainer> StartAppContainerAsync(string containerImageTag, string seededDbFilePath)
    {
        Console.WriteLine("Building and starting Docker network");
        Console.WriteLine("Building and starting app container");

        var container = new ContainerBuilder($"shopandeat:{containerImageTag}-chiseled")
            .WithNetwork(_network)
            .WithNetworkAliases(AppNetworkAlias)
            .WithPortBinding(int.Parse(AppContainerPort, CultureInfo.InvariantCulture), assignRandomHostPort: true)
            .WithResourceMapping(
                new FileInfo(seededDbFilePath),
                AppDbDirectoryInContainer,
                AppContainerUid,
                AppContainerUid,
                UnixFileModes.UserRead | UnixFileModes.UserWrite)
            .WithEnvironment("ConnectionStrings__SQLite", $"Data Source={AppDbPathInContainer}")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Content root path: /app",
                    strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30))))
            .Build();

        await container.StartAsync(_cancellationTokenSource.Token);
        Console.WriteLine("App container started");

        return container;
    }

    private async Task Cleanup()
    {
        await _cancellationTokenSource.CancelAsync();

        // Cleanup must use its own token: the token above is deliberately cancelled to stop any
        // in-flight test operations, but Testcontainers cleanup calls need a fresh, non-cancelled token.
        using var cleanupCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        if (_playwrightSession != null)
        {
            await _playwrightSession.DisposeAsync();
        }

        if (_appContainer != null)
        {
            await _appContainer.StopAsync(cleanupCancellationTokenSource.Token);

            // Only delete the image when tests passed (keep for investigation on failure)
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                (TestContext.CurrentContext?.Result.Outcome.Status != TestStatus.Passed))
            {
                await _appContainer.DisposeAsync();
            }
            else
            {
                await CleanupAppImageAsync(_appContainer, cleanupCancellationTokenSource.Token);
                await _appContainer.DisposeAsync();
            }
        }

        if (_network != null)
        {
            await _network.DisposeAsync();
        }

        DeleteSeededDatabaseDirectory();

        _cancellationTokenSource.Dispose();
    }

    private void DeleteSeededDatabaseDirectory()
    {
        if (string.IsNullOrWhiteSpace(_seededDbFilePath))
        {
            return;
        }

        try
        {
            var seededDbDirectory = Path.GetDirectoryName(_seededDbFilePath);
            if (!string.IsNullOrWhiteSpace(seededDbDirectory) && Directory.Exists(seededDbDirectory))
            {
                Directory.Delete(seededDbDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
