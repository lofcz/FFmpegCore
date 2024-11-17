namespace FFmpegCore
{
    public interface IArgument
    {
        string Argument { get; }
        string Name => Argument;
    }

    public interface IInputArgument : IArgument, IHasMetaData
    {
        public bool UseStandardInput => false;
    }

    public interface IOutputArgument : IArgument
    {
    }
}
