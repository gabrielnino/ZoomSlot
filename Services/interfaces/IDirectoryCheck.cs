namespace Services.interfaces
{
    public interface IDirectoryCheck
    {
        void EnsureDirectoryExists(string path);
    }
}
