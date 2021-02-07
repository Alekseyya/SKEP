namespace Core.BL.Interfaces
{
    public interface IOOService
    {
        byte[] DownloadFile(string fileUrl, string login, string password);
        bool CheckPayrollAccess();
        bool UploadFileVersion(string fileUrl, string login, string password, byte[] fileBinData);
        void UploadFile(string url, byte[] fileBinData, string fileName, string contentType, string apiToken);
    }
}
