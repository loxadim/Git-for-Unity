using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BaseTests;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using TestUtils.Events;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    [TestFixture, Category("DoNotRunOnAppVeyor")]
    class RepositoryManagerTests : BaseTest
    {
        [Test]
        public async Task ShouldDetectFileChanges()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);

                await events.WaitForNotBusy();
                listener.ClearReceivedCalls();
                events.Reset();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                var r = await ProcessEvents(events);
                test.Logger.Info(r.Join(Environment.NewLine));

                // we expect these events
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                listener.ClearReceivedCalls();
                events.Reset();

                var filesToCommit = new List<string> { "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                await test.RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                await ProcessEvents(events);

                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.CommitAllFiles("IntegrationTest Commit", string.Empty).StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.SwitchBranch("feature/document").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                test.Logger.Info((await ProcessEvents(events)).Join(Environment.NewLine));

                // we expect these events
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
                //await TaskManager.Wait();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                {
                    // prepopulate repository info cache
                    var b = test.Repository.CurrentBranch;
                    test.RepositoryManager.WaitForEvents();
                    await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);
                    listener.ClearReceivedCalls();
                    events.Reset();
                }

                var createdBranch1 = "feature/document2";
                await test.RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
                //await TaskManager.Wait();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);

                // we don't expect these events
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(events.GitLogUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(events.GitLogUpdated);
                await AssertDidNotReceiveEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.RemoteRemove("origin").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            using (var test = StartTest(TestData.TestRepoMasterTwoRemotes))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.CreateBranch("branch2", "another/master").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.SwitchBranch("branch2").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.Pull("origin", "master").StartAsAsync();
                //await TaskManager.Wait();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.GitLogUpdated), events.GitLogUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                // TODO: this should not happen but it's happening right now because when local branches get updated in the cache, remotes get updated too
                //await AssertDidNotReceiveEvent(events.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.Fetch("origin").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                await ProcessEvents(events);

                // we expect these events
                await AssertReceivedEvent(nameof(events.LocalBranchesUpdated), events.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), events.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), events.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(events.CurrentBranchUpdated), events.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), events.GitStatusUpdated);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(events.GitLogUpdated);
                //await AssertDidNotReceiveEvent(events.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), events.GitLocksUpdated);
            }
        }

        private async Task AssertReceivedEvent(string eventName, Task task)
        {
            try
            {
                (await Task.WhenAny(task, Task.Delay(1))).Should().BeAssignableTo<Task<object>>("otherwise the event was not raised");
            }
            catch
            {
                throw new Exception($"Event {eventName} should have been raised");
            }
        }

        private async Task AssertDidNotReceiveEvent(string eventName, Task task)
        {
            try
            {
                (await Task.WhenAny(task, Task.Delay(1))).Should().BeAssignableTo<Task<bool>>("otherwise the event was raised");
            }
            catch
            {
                throw new Exception($"Event {eventName} should not have been raised");
            }
        }

        private async Task<List<string>> ProcessEvents(RepositoryManagerEvents events)
        {
            int timeout = 500;
            var received = new List<string>
            {
                $"CurrentBranchUpdated: {(await Task.WhenAny(events.CurrentBranchUpdated, Task.Delay(timeout))) is Task<object>}",
                $"GitAheadBehindStatusUpdated: {(await Task.WhenAny(events.GitAheadBehindStatusUpdated, Task.Delay(timeout))) is Task<object>}",
                $"GitLocksUpdated: {(await Task.WhenAny(events.GitLocksUpdated, Task.Delay(timeout))) is Task<object>}",
                $"GitLogUpdated: {(await Task.WhenAny(events.GitLogUpdated, Task.Delay(timeout))) is Task<object>}",
                $"GitStatusUpdated: {(await Task.WhenAny(events.GitStatusUpdated, Task.Delay(timeout))) is Task<object>}",
                $"LocalBranchesUpdated: {(await Task.WhenAny(events.LocalBranchesUpdated, Task.Delay(timeout))) is Task<object>}",
                $"RemoteBranchesUpdated: {(await Task.WhenAny(events.RemoteBranchesUpdated, Task.Delay(timeout))) is Task<object>}",
            };
            return received;
        }

    }
}
