using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NCrunch.Framework;
using NUnit.Framework;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest : BaseTest
    {
        protected SPath TestApp => TestLocation.Combine("CommandLine.exe");
        public IRepositoryManager RepositoryManager { get; set; }
        protected IApplicationManager ApplicationManager { get; set; }
        protected ITaskManager TaskManager { get; set; }
        protected IPlatform Platform { get; set; }
        protected IProcessManager ProcessManager { get; set; }
        protected IProcessEnvironment ProcessEnvironment => Platform.ProcessEnvironment;
        protected IGitClient GitClient { get; set; }
        public IntegrationTestEnvironment Environment { get; set; }

        protected SPath DotGitConfig { get; set; }
        protected SPath DotGitHead { get; set; }
        protected SPath DotGitIndex { get; set; }
        protected SPath RemotesPath { get; set; }
        protected SPath BranchesPath { get; set; }
        protected SPath DotGitPath { get; set; }
        protected SPath TestRepoMasterCleanSynchronized { get; set; }
        protected SPath TestRepoMasterCleanUnsynchronized { get; set; }
        protected SPath TestRepoMasterCleanUnsynchronizedRussianLanguage { get; set; }
        protected SPath TestRepoMasterDirtyUnsynchronized { get; set; }
        protected SPath TestRepoMasterTwoRemotes { get; set; }

        protected static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        public IRepository Repository => Environment.Repository;

        protected void InitializeEnvironment(SPath repoPath,
            bool enableEnvironmentTrace = false,
            bool initializeRepository = true
            )
        {
            var environment = CreateEnvironmentInPersistentLocation(repoPath, enableEnvironmentTrace);
            if (initializeRepository)
                environment.InitializeRepository(repoPath);

            environment.NodeJsExecutablePath = TestApp;
            environment.OctorunScriptPath = TestApp;
            Environment = environment;
        }

        protected void InitializePlatform(SPath repoPath,
            bool enableEnvironmentTrace = true,
            string testName = "")
        {
            InitializeTaskManager();

            ProcessManager = new ProcessManager(Environment);
            Platform = new Platform(TaskManager, Environment, ProcessManager);

            Platform.Initialize();
        }

        protected override ITaskManager InitializeTaskManager()
        {
            TaskManager = base.InitializeTaskManager();
            ApplicationManager = new ApplicationManagerBase(SyncContext, Environment);
            return TaskManager;
        }

        protected IGitEnvironment InitializePlatformAndEnvironment(SPath repoPath,
            bool enableEnvironmentTrace = false,
            Action<IRepositoryManager> onRepositoryManagerCreated = null,
            [CallerMemberName] string testName = "")
        {
            InitializeEnvironment(repoPath, enableEnvironmentTrace, true);
            InitializePlatform(repoPath, enableEnvironmentTrace: enableEnvironmentTrace, testName: testName);
            SetupGit(Environment.UserCachePath, testName);

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath = DotGitPath.ReadAllLines().Where(x => x.StartsWith("gitdir:"))
                                       .Select(x => x.Substring(7).Trim().ToSPath()).First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");

            RepositoryManager = Unity.VersionControl.Git.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, repoPath);
            RepositoryManager.Initialize();

            onRepositoryManagerCreated?.Invoke(RepositoryManager);

            Environment.Repository?.Initialize(RepositoryManager, TaskManager);

            RepositoryManager.Start();
            Environment.Repository?.Start();
            return Environment;
        }

        protected void SetupGit(SPath pathToSetupGitInto, string testName)
        {
            var installDetails = new GitInstaller.GitInstallDetails(pathToSetupGitInto, Environment);
            var state = installDetails.GetDefaults();
            Environment.GitInstallationState = state;
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);

            if (installDetails.GitExecutablePath.FileExists() && installDetails.GitLfsExecutablePath.FileExists())
                return;

            var key = installDetails.GitManifest.FileNameWithoutExtension + "_updatelastCheckTime";
            Environment.UserSettings.Set(key, DateTimeOffset.Now);

            var localCache = TestLocation.Combine("Resources");
            localCache.CopyFiles(pathToSetupGitInto, true);
            // skip checking for updates

            state.GitPackage = DugiteReleaseManifest.Load(installDetails.GitManifest, GitInstaller.GitInstallDetails.GitPackageFeed, Environment);
            var asset = state.GitPackage.DugitePackage;
            state.GitZipPath = installDetails.ZipPath.Combine(asset.Name);

            installDetails.GitInstallationPath.DeleteIfExists();

            state.GitZipPath.EnsureParentDirectoryExists();

            var gitExtractPath = TestBasePath.Combine("setup", "git_zip_extract_zip_paths").EnsureDirectoryExists();
            var source = new UnzipTask(TaskManager.Token, state.GitZipPath, gitExtractPath, null, Environment.FileSystem)
                            .RunSynchronously();

            installDetails.GitInstallationPath.EnsureParentDirectoryExists();
            source.Move(installDetails.GitInstallationPath);
        }

        public override void OnSetup()
        {
            base.OnSetup();

            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            InitializeTaskManager();
        }

        [TearDown]
        public override void OnTearDown()
        {
            TaskManager.Dispose();
            Environment?.CacheContainer.Dispose();
            BranchesCache.Instance = null;
            GitAheadBehindCache.Instance = null;
            GitLocksCache.Instance = null;
            GitLogCache.Instance = null;
            GitStatusCache.Instance = null;
            GitUserCache.Instance = null;
            RepositoryInfoCache.Instance = null;

            base.OnTearDown();
        }
    }
}
