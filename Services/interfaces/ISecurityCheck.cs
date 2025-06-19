namespace Services.interfaces
{
    public interface ISecurityCheck
    {
        bool IsSecurityChek();
        Task TryStartPuzzle();
        Task HandleSecurityPage();
        Task HandleUnexpectedPage();
    }
}
