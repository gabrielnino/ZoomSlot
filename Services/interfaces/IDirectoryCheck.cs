namespace Services.Interfaces
{
    public interface IDirectoryCheck
    {
        void EnsureDirectoryExists(string path);
    }
}
