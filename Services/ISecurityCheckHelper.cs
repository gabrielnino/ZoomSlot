namespace Services
{
    public interface ISecurityCheckHelper
    {
        bool IsSecurityChek();
        Task TryStartPuzzle();
        Task HandleSecurityPage();
        Task HandleUnexpectedPage();
    }
}
