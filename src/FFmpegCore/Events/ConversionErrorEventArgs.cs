using FFmpegCore.Exceptions;
using System;

namespace FFmpegCore.Events
{
    public class ConversionErrorEventArgs : EventArgs
    {
        public ConversionErrorEventArgs(FFmpegException exception, IInputArgument input, IOutputArgument output)
        {
            Exception = exception;
            Input = input;
            Output = output;
        }

        public FFmpegException Exception { get; }
        public IInputArgument Input { get; }
        public IOutputArgument Output { get; }
    }
}