using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitFetchTask : NativeProcessTask<string>
    {
        private const string TaskName = "git fetch";
        private readonly string arguments;

        public GitFetchTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string remote, bool prune = true, bool tags = true,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            var args = new List<string> { "fetch" };
            
            if (prune)
            {
                args.Add("--prune");
            }

            if (tags)
            {
                args.Add("--tags");
            }

            if (!String.IsNullOrEmpty(remote))
            {
                args.Add(remote);
            }

            arguments = args.Join(" ");
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Fetching...";
    }
}
