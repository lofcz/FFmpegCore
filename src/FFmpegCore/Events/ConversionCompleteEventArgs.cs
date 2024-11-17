using System;

namespace FFmpegCore.Events
{
    public class ConversionCompleteEventArgs : EventArgs
    {
        public ConversionCompleteEventArgs(IInputArgument input, IOutputArgument output)
        {
            Input = input;
            Output = output;
        }

        public IInputArgument Input { get; }
        public IOutputArgument Output { get; }
    }
}