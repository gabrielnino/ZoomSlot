namespace Services
{
    public class JobCommandArgs(string[] args)
    {
        public string MainCommand { get; } = args.FirstOrDefault(arg => arg == "--search" || arg == "--export") ?? "--help";
    }
}
