namespace Services
{
    public interface IDirectoryCheck
    {
        void EnsureDirectoryExists(string path);
    }
}
