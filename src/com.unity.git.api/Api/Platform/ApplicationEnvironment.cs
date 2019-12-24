using Unity.VersionControl.Git;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;

namespace Unity.VersionControl.Git
{
    using IO;

    public class ApplicationEnvironment : UnityEnvironment, IGitEnvironment
    {
        private const string logFile = "github-unity.log";

        public ApplicationEnvironment(string applicationName = null) : base(applicationName ?? ApplicationInfo.ApplicationName)
        {
            LocalAppData = GetFolder(Folders.LocalApplicationData);
            CommonAppData = GetFolder(Folders.CommonApplicationData);

            UserCachePath = LocalAppData.Combine(applicationName).EnsureDirectoryExists();
            SystemCachePath = CommonAppData.Combine(applicationName).EnsureDirectoryExists();

            LogPath = GetFolder(Folders.Logs).Combine(applicationName).EnsureDirectoryExists().Combine(logFile);
            GitDefaultInstallation = new GitInstaller.GitInstallDetails(UserCachePath, this);
        }

        public ApplicationEnvironment(ICacheContainer cacheContainer, string applicationName = null)
            : this(applicationName)
        {
            this.CacheContainer = cacheContainer;
        }

        /// <summary>
        /// This is for tests to reset the static OS flags
        /// </summary>
        public static void Reset()
        {
            //onWindows = null;
            //onLinux = null;
            //onMac = null;
        }

        public IGitEnvironment Initialize(SPath extensionInstallPath, string projectPath, string unityVersion = null, string EditorApplication_applicationPath = null, string EditorApplication_applicationContentsPath = null)
        {
            base.Initialize(projectPath, unityVersion, EditorApplication_applicationPath, EditorApplication_applicationContentsPath);
            ExtensionInstallPath = extensionInstallPath;
            User = new User(CacheContainer);
            UserSettings = new UserSettings(this);
            LocalSettings = new LocalSettings(this);
            SystemSettings = new SystemSettings(this);
            return this;
        }

        public void InitializeRepository(SPath? repositoryPath = null)
        {
            SPath expectedRepositoryPath;
            if (!RepositoryPath.IsInitialized || (repositoryPath != null && RepositoryPath != repositoryPath.Value))
            {
                Guard.NotNull(this, UnityProjectPath, nameof(UnityProjectPath));

                expectedRepositoryPath = repositoryPath != null ? repositoryPath.Value : UnityProjectPath.ToSPath();

                if (!expectedRepositoryPath.Exists(".git"))
                {
                    SPath reporoot = UnityProjectPath.ToSPath().RecursiveParents.FirstOrDefault(d => d.Exists(".git"));
                    if (reporoot.IsInitialized)
                        expectedRepositoryPath = reporoot;
                }
            }
            else
            {
                expectedRepositoryPath = RepositoryPath;
            }

            if (expectedRepositoryPath.Exists(".git"))
            {
                SPath.FileSystem = new FileSystem(expectedRepositoryPath);
                RepositoryPath = expectedRepositoryPath;
                Repository = new Repository(RepositoryPath, CacheContainer);
            }
        }

        public SPath GetFolder(Folders folder)
        {
            switch (folder)
            {
                case Folders.LocalApplicationData:
                {
                    if (IsMac)
                        return SPath.HomeDirectory.Combine("Library", "Application Support");
                }
                break;
                case Folders.CommonApplicationData:
                {
                    if (IsLinux)
                        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                                          .ToSPath();

                    return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                                      .ToSPath();
                }
                break;
                case Folders.Logs:
                {
                    if (IsMac)
                        return SPath.HomeDirectory.Combine("Library/Logs");
                }
                break;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToSPath();
        }

        public SPath LogPath { get; }
        public SPath ExtensionInstallPath { get; set; }
        public SPath UserCachePath { get; set; }
        public SPath SystemCachePath { get; set; }
        public SPath LocalAppData { get; set; }
        public SPath CommonAppData { get; set; }

        public bool IsCustomGitExecutable => GitInstallationState?.IsCustomGitPath ?? false;
        public SPath GitInstallPath => GitInstallationState?.GitInstallationPath ?? SPath.Default;
        public SPath GitExecutablePath => GitInstallationState?.GitExecutablePath ?? SPath.Default;
        public SPath GitLfsInstallPath => GitInstallationState?.GitLfsInstallationPath ?? SPath.Default;
        public SPath GitLfsExecutablePath => GitInstallationState?.GitLfsExecutablePath ?? SPath.Default;
        public GitInstaller.GitInstallationState GitInstallationState
        {
            get
            {
                return SystemSettings.Get<GitInstaller.GitInstallationState>(Constants.GitInstallationState, new GitInstaller.GitInstallationState());
            }
            set
            {
                if (value == null)
                    SystemSettings.Unset(Constants.GitInstallationState);
                else
                    SystemSettings.Set<GitInstaller.GitInstallationState>(Constants.GitInstallationState, value);
            }
        }

        public GitInstaller.GitInstallDetails GitDefaultInstallation { get; set; }

        public SPath RepositoryPath { get; private set; }
        public ICacheContainer CacheContainer { get; private set; }
        public IRepository Repository { get; set; }
        public IUser User { get; set; }
        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        protected static ILogging Logger { get; } = LogHelper.GetLogger<ApplicationEnvironment>();
    }
}
