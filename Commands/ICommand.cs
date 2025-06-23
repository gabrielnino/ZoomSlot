namespace Commands
{
    public interface ICommand
    {
        Task ExecuteAsync(Dictionary<string, string>? arguments=null);
    }
}
