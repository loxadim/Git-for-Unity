﻿using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitCommitTask : NativeProcessTask<string>
    {
        private const string TaskName = "git commit";

        private readonly string message;
        private readonly string body;
        private readonly string arguments;

        private SPath tempFile;

        public GitCommitTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string message, string body,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(message, "message");

            this.message = message;
            this.body = body ?? string.Empty;

            Name = TaskName;
            tempFile = SPath.GetTempFilename("GitCommitTask");
            arguments = $"-c i18n.commitencoding=utf8 commit --file \"{tempFile}\"";
        }

        protected override void RaiseOnStart()
        {
            base.RaiseOnStart();
            tempFile.WriteAllLines(new [] { message, Environment.NewLine, body });
        }

        protected override void RaiseOnEnd()
        {
            tempFile.DeleteIfExists();
            base.RaiseOnEnd();
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Committing...";
    }
}
