﻿using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FFmpegCore
{
    internal interface IProcessExecutionHandler
    {
        public Task HandleProcessStartedAsync(Process process, CancellationToken cancellationToken);
        public Task HandleProcessExitedAsync(Process process, CancellationToken cancellationToken);
    }
}