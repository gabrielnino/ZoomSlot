namespace Services.Interfaces
{
    public interface ISecurityCheck
    {
        bool IsSecurityChek();
        Task TryStartPuzzle();
        Task HandleSecurityPage();
        Task HandleUnexpectedPage();
    }
}
