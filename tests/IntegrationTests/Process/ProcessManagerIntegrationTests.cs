//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using FluentAssertions;
//using NUnit.Framework;
//using Unity.VersionControl.Git;
//using TestUtils;
//using System.Threading.Tasks;
//using Unity.VersionControl.Git.IO;
//using Unity.VersionControl.Git.Tasks;
//using Unity.Editor.Tasks;

//namespace IntegrationTests
//{
//    [TestFixture]
//    class ProcessManagerIntegrationTests : BaseGitEnvironmentTest
//    {
//        [Test]
//        [Category("DoNotRunOnAppVeyor")]
//        public async Task BranchListTest()
//        {
//            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized);

//            var gitBranches = await new GitListLocalBranchesTask(TaskManager, ProcessEnvironment, Environment)
//                                    .Configure(ProcessManager, TestRepoMasterCleanUnsynchronized)
//                                    .StartAsAsync();

//            gitBranches.Should().BeEquivalentTo(
//                new GitBranch("master", "origin/master"),
//                new GitBranch("feature/document", "origin/feature/document"));
//        }

//        [Test]
//        public async Task LogEntriesTest()
//        {
//            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized);

//            var logEntries = await new GitLogTask(TaskManager, ProcessEnvironment, Environment, new GitObjectFactory(Environment), 2)
//                               .Configure(ProcessManager, TestRepoMasterCleanUnsynchronized)
//                               .StartAsAsync();

//            var firstCommitTime = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5));
//            var secondCommitTime = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8));

//            logEntries.AssertEqual(new[]
//            {
//                new GitLogEntry("018997938335742f8be694240a7c2b352ec0835f",
//                    "Author Person", "author@example.com", "Author Person",
//                    "author@example.com",
//                    "Moving project files where they should be kept",
//                    "",
//                    firstCommitTime,
//                    firstCommitTime, new List<GitStatusEntry>
//                    {
//                        new GitStatusEntry("Assets/TestDocument.txt".ToSPath(),
//                            TestRepoMasterCleanUnsynchronized + "/Assets/TestDocument.txt".ToSPath(), "Assets/TestDocument.txt".ToSPath(),
//                            GitFileStatus.Renamed, GitFileStatus.None, "TestDocument.txt")
//                    }),

//                new GitLogEntry("03939ffb3eb8486dba0259b43db00842bbe6eca1",
//                    "Author Person", "author@example.com", "Author Person",
//                    "author@example.com",
//                    "Initial Commit",
//                    "",
//                    secondCommitTime,
//                    secondCommitTime, new List<GitStatusEntry>
//                    {
//                        new GitStatusEntry("TestDocument.txt".ToSPath(),
//                            TestRepoMasterCleanUnsynchronized + "/TestDocument.txt".ToSPath(), "TestDocument.txt".ToSPath(),
//                            GitFileStatus.Added, GitFileStatus.None),
//                    }),
//            });
//        }

//        [Test]
//        public async Task RussianLogEntriesTest()
//        {
//            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronizedRussianLanguage);

//            var logEntries = await new GitLogTask(TaskManager, ProcessEnvironment, Environment, new GitObjectFactory(Environment), 1)
//                .Configure(ProcessManager, TestRepoMasterCleanUnsynchronizedRussianLanguage)
//                .StartAsAsync();

//            var commitTime = new DateTimeOffset(2017, 4, 20, 11, 47, 18, TimeSpan.FromHours(-4));

//            logEntries.AssertEqual(new[]
//            {
//                new GitLogEntry("06d6451d351626894a30e9134f551db12c74254b",
//                    "Author Person", "author@example.com", "Author Person",
//                    "author@example.com",
//                    "Я люблю github",
//                    "",
//                    commitTime,
//                    commitTime, new List<GitStatusEntry>
//                    {
//                        new GitStatusEntry(@"Assets\A new file.txt".ToSPath(),
//                            TestRepoMasterCleanUnsynchronizedRussianLanguage + "/Assets/A new file.txt".ToSPath(), "Assets/A new file.txt".ToSPath(),
//                            GitFileStatus.Added, GitFileStatus.None),
//                    }),
//            });
//        }

//        [Test]
//        public async Task RemoteListTest()
//        {
//            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

//            var gitRemotes = await new GitListRemoteBranchesTask(TaskManager, ProcessEnvironment, Environment)
//                                   .Configure(ProcessManager, TestRepoMasterCleanUnsynchronized)
//                                   .StartAsAsync();

//            gitRemotes.Should().BeEquivalentTo(new GitRemote("origin", "github.com", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git", GitRemoteFunction.Both));
//        }

//        [Test]
//        public async Task StatusTest()
//        {
//            InitializePlatformAndEnvironment(TestRepoMasterDirtyUnsynchronized);


//            var gitStatus = await new GitStatusTask(TaskManager, ProcessEnvironment, Environment, new GitObjectFactory(Environment))
//                                  .Configure(ProcessManager, TestRepoMasterCleanUnsynchronized)
//                                  .StartAsAsync();

//            gitStatus.AssertEqual(new GitStatus()
//            {
//                LocalBranch = "master",
//                RemoteBranch = "origin/master",
//                Behind = 1,
//                Entries = new List<GitStatusEntry>
//                {
//                    new GitStatusEntry("Assets/Added Document.txt".ToSPath(),
//                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Added Document.txt"),
//                        "Assets/Added Document.txt".ToSPath(),
//                        GitFileStatus.Added, GitFileStatus.None),

//                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToSPath(),
//                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Renamed TestDocument.txt"),
//                        "Assets/Renamed TestDocument.txt".ToSPath(),
//                        GitFileStatus.Renamed,  GitFileStatus.None, "Assets/TestDocument.txt".ToSPath()),

//                    new GitStatusEntry("Assets/Untracked Document.txt".ToSPath(),
//                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Untracked Document.txt"),
//                        "Assets/Untracked Document.txt".ToSPath(),
//                        GitFileStatus.Untracked, GitFileStatus.Untracked),
//                }
//            });
//        }

//        //[Test]
//        //public async Task CredentialHelperGetTest()
//        //{
//        //    InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

//        //    await ProcessManager
//        //        .GetGitCreds(TestRepoMasterCleanSynchronized)
//        //        .StartAsAsync();
//        //}
//    }
//}
