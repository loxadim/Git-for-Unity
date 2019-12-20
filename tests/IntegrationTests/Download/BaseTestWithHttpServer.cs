using System.Threading.Tasks;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    class BaseTestWithHttpServer
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public void TestFixtureSetUp()
        {
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public void TestFixtureTearDown()
        {
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }


    class BaseIntegrationTestWithHttpServer
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public void TestFixtureSetUp()
        {
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public void TestFixtureTearDown()
        {
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }

    class BaseGitTestWithHttpServer
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public void TestFixtureSetUp()
        {
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public void TestFixtureTearDown()
        {
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }
}
