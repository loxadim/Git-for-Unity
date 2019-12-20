using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaseTests;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests.Download
{
    class TestsWithHttpServer : BaseTest
    {
        [Test]
        public async Task DownloadAndVerificationWorks()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var file = "unity/latest.json";
                var package = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/{file}"));

                var downloader = new Downloader(test.TaskManager);
                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                Assert.IsTrue(downloader.Successful);
                var result = await downloader.Task;

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(test.SourceDirectory.Combine("files/unity/releases/github-for-unity-99.2.0-beta1.unitypackage").CalculateMD5(), result[0].File.ToSPath().CalculateMD5());
            }
        }

        [Test]
        public async Task DownloadingNonExistingFileThrows()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var package = new Package { Url = $"http://localhost:{test.HttpServer.Port}/nope" };

                var downloader = new Downloader(test.TaskManager);
                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));
                Assert.AreEqual(downloader.Task, task);
                Func<Task> act = async () => await downloader.Task;
                await act.Should().ThrowAsync<DownloadException>();
            }
        }

        [Test]
        public async Task FailsIfVerificationFails()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var gitPackage = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git.json"));
                var gitLfsPackage = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git-lfs.json"));

                var package = new Package { Url = gitPackage.Url, Md5 = gitLfsPackage.Md5 };

                var downloader = new Downloader(test.TaskManager);
                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                Assert.IsTrue(downloader.Successful);
                var result = await downloader.Task;

                Assert.AreNotEqual(package.Md5, result[0].File.ToSPath().CalculateMD5());
            }
        }

        [Test]
        public async Task ResumingWorks()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var fileSystem = SPath.FileSystem;
                var package = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/latest.json"));

                var downloader = new Downloader(test.TaskManager);
                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));
                Assert.AreEqual(downloader.Task, task);
                var result = await downloader.Task;
                var downloadData = result.FirstOrDefault();

                var downloadPathBytes = fileSystem.ReadAllBytes(downloadData.File);

                var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();
                fileSystem.FileDelete(downloadData.File);
                fileSystem.WriteAllBytes(downloadData.File + ".partial", cutDownloadPathBytes);

                downloader = new Downloader(test.TaskManager);

                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);
                task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                result = await downloader.Task;
                downloadData = result.FirstOrDefault();

                var md5Sum = downloadData.File.ToSPath().CalculateMD5();
                md5Sum.Should().BeEquivalentTo(package.Md5);
            }
        }

        [Test]
        public async Task SucceedIfEverythingIsAlreadyDownloaded()
        {
            using (var test = StartTest(withHttpServer: true))
            {

                var fileSystem = SPath.FileSystem;
                var package = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/latest.json"));

                var downloader = new Downloader(test.TaskManager);

                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                var downloadData = await downloader.Task;
                var downloadPath = downloadData.FirstOrDefault().File.ToSPath();

                downloader = new Downloader(test.TaskManager);

                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);
                task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                downloadData = await downloader.Task;
                downloadPath = downloadData.FirstOrDefault().File.ToSPath();

                var md5Sum = downloadPath.CalculateMD5();
                md5Sum.Should().BeEquivalentTo(package.Md5);
            }
        }

        [Category("DoNotRunOnAppVeyor")]
        public async Task DownloadsRunSideBySide()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var package1 = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git-lfs.json"));
                var package2 = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git.json"));

                var events = new List<string>();

                var downloader = new Downloader(test.TaskManager);

                downloader.QueueDownload(package2.Uri, test.TestPath);
                downloader.QueueDownload(package1.Uri, test.TestPath);
                downloader.OnDownloadStart += url => events.Add("start " + url.Filename);
                downloader.OnDownloadComplete += (url, file) => events.Add("end " + url.Filename);
                downloader.OnDownloadFailed += (url, ex) => events.Add("failed " + url.Filename);

                test.HttpServer.Delay = 1;

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));
                test.HttpServer.Delay = 0;

                Assert.AreEqual(downloader.Task, task);

                CollectionAssert.AreEqual(new string[] { "start git.zip", "start git-lfs.zip", "end git-lfs.zip", "end git.zip", }, events);
            }
        }

        [Test]
        public async Task ResumingDownloadsWorks()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var fileSystem = SPath.FileSystem;

                var baseUrl = new UriString($"http://localhost:{test.HttpServer.Port}/unity");
                var package = Package.Load(test.TaskManager, test.Environment, baseUrl.ToString() + "/latest.json");

                var downloadTask = new DownloadTask(test.TaskManager, package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloadTask.Start().Task, Task.Delay(Timeout));

                task.Should().BeEquivalentTo(downloadTask.Task);

                var downloadPath = (await downloadTask.Task).ToSPath();
                Assert.NotNull(downloadPath);

                var downloadPathBytes = downloadPath.ReadAllBytes();

                var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();

                downloadPath.Delete();

                new SPath(downloadPath + ".partial").WriteAllBytes(cutDownloadPathBytes);

                downloadTask = new DownloadTask(test.TaskManager, package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                task = await Task.WhenAny(downloadTask.Start().Task, Task.Delay(Timeout));

                task.Should().BeEquivalentTo(downloadTask.Task);
                downloadPath = (await downloadTask.Task).ToSPath();

                var md5Sum = downloadPath.CalculateMD5();
                md5Sum.Should().BeEquivalentTo(package.Md5);
            }
        }

        [Test]
        public void DownloadingFromNonExistingDomainThrows()
        {
            using (var test = StartTest(withHttpServer: true))
            {

                var fileSystem = SPath.FileSystem;

                var downloadTask = new DownloadTask(test.TaskManager, "http://ggggithub.com/robots.txt", test.TestPath);
                var exceptionThrown = false;

                var autoResetEvent = new AutoResetEvent(false);

                downloadTask.FinallyInline(success => {
                                exceptionThrown = !success;
                                autoResetEvent.Set();
                            })
                            .Start();

                autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");

                exceptionThrown.Should().BeTrue();
            }
        }


        [Test]
        public void ShutdownTimeWhenTaskManagerDisposed()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                test.HttpServer.Delay = 100;

                var fileSystem = SPath.FileSystem;

                var evtStop = new AutoResetEvent(false);
                var evtFinally = new AutoResetEvent(false);
                Exception exception = null;

                var gitLfs = new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git-lfs.zip");

                var downloadGitTask = new DownloadTask(test.TaskManager, gitLfs, test.TestPath)

                                      // An exception is thrown when we stop the task manager
                                      // since we're stopping the task manager, no other tasks
                                      // will run, which means we can only hook with Catch
                                      // or with the Finally overload that runs on the same thread (not as a task)
                                      .Catch(e => {
                                          exception = e;
                                          evtFinally.Set();
                                      })
                                      .Progress(p => { evtStop.Set(); });

                downloadGitTask.Start();

                evtStop.WaitOne(Timeout).Should().BeTrue("Progress raised the signal");

                test.TaskManager.Dispose();

                evtFinally.WaitOne(Timeout).Should().BeTrue("Catch raised the signal");

                test.HttpServer.Delay = 0;
                test.HttpServer.Abort();

                exception.Should().NotBeNull();
            }
        }
    }
}
