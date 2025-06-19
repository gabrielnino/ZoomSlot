namespace Services.Interfaces
{
    public interface ISecurityCheck
    {
        bool IsSecurityCheck();
        Task TryStartPuzzle();
        Task HandleSecurityPage();
        Task HandleUnexpectedPage();
    }
}
