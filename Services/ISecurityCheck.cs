namespace Services
{
    public interface ISecurityCheck
    {
        bool IsSecurityChek();
        Task TryStartPuzzle();
        Task HandleSecurityPage();
        Task HandleUnexpectedPage();
    }
}
