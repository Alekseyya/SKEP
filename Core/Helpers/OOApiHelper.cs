﻿namespace Core.Helpers
{
    //public class OOApiHelper
    //{
    //    private static IApplicationUserService _applicationUserService;
    //    private static IFinanceService _financeService;
    //    public static void Configure(IServiceProvider serviceProvider)
    //    {
    //        _applicationUserService = serviceProvider.GetService<IApplicationUserService>();
    //        _financeService = serviceProvider.GetService<IFinanceService>();
    //    }
    //    public static byte[] DownloadFile(string fileUrl, string login, string password)
    //    {
    //        byte[] fileBinData = null;

    //        Uri fileUri = new Uri(fileUrl);
    //        string fileId = HttpUtility.ParseQueryString(fileUri.Query).Get("fileid");

    //        string apiToken = GetApiToken(fileUri.Scheme + "://" + fileUri.Host + "/api/2.0/authentication.json", login, password);

    //        //Console.WriteLine(apiToken);

    //        if (String.IsNullOrEmpty(apiToken) == false)
    //        {
    //            byte[] result = CreateRequest(fileUri.Scheme + "://" + fileUri.Host + "/api/2.0/files/file/" + fileId,
    //                "GET", apiToken, null);

    //            Dictionary<string, Object> responseDictionary = null;

    //            if (result != null)
    //            {
    //                string sstr = Encoding.UTF8.GetString(result);
    //                responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(sstr);

    //                if (responseDictionary.ContainsKey("response") == true
    //                    && String.IsNullOrEmpty(responseDictionary["response"].ToString()) == false)
    //                {
    //                    Dictionary<string, Object> responseDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseDictionary["response"].ToString());

    //                    string fileExst = responseDataDictionary["fileExst"].ToString();
    //                    fileBinData = DownloadFileInternal(fileUri.Scheme + "://" + fileUri.Host + "/products/files/httphandlers/filehandler.ashx?action=download&fileid=" + fileId + "&outputtype=" + fileExst, apiToken);
    //                }
    //            }
    //        }

    //        return fileBinData;
    //    }

    //    public static bool UploadFileVersion(string fileUrl, string login, string password, byte[] fileBinData)
    //    {
    //        bool result = false;

    //        Uri fileUri = new Uri(fileUrl);
    //        string fileId = HttpUtility.ParseQueryString(fileUri.Query).Get("fileid");

    //        string apiToken = GetApiToken(fileUri.Scheme + "://" + fileUri.Host + "/api/2.0/authentication.json", login, password);

    //        //Console.WriteLine(apiToken);

    //        if (String.IsNullOrEmpty(apiToken) == false)
    //        {
    //            byte[] requestResult = CreateRequest(fileUri.Scheme + "://" + fileUri.Host + "/api/2.0/files/file/" + fileId,
    //               "GET", apiToken, null);

    //            Dictionary<string, Object> responseDictionary = null;

    //            if (requestResult != null)
    //            {
    //                string sstr = Encoding.UTF8.GetString(requestResult);
    //                responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(sstr);

    //                if (responseDictionary.ContainsKey("response") == true
    //                    && String.IsNullOrEmpty(responseDictionary["response"].ToString()) == false)
    //                {
    //                    Dictionary<string, Object> responseDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseDictionary["response"].ToString());

    //                    string folderId = responseDataDictionary["folderId"].ToString();
    //                    string fileName = responseDataDictionary["title"].ToString();

    //                    UploadFile(fileUri.Scheme + "://" + fileUri.Host + "/api/2.0/files/" + folderId + "/upload",
    //                        fileBinData, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", apiToken);

    //                    result = true;
    //                }
    //            }
    //        }

    //        return result;
    //    }

    //    public static void UploadFile(string url, byte[] fileBinData, string fileName, string contentType,
    //        string apiToken)
    //    {
    //        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
    //        byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

    //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //        request.Headers.Add("Authorization", apiToken);
    //        request.ContentType = "multipart/form-data; boundary=" + boundary;
    //        request.Method = "POST";
    //        request.KeepAlive = true;
    //        request.Credentials = System.Net.CredentialCache.DefaultCredentials;

    //        Stream requestStream = request.GetRequestStream();
    //        requestStream.Write(boundarybytes, 0, boundarybytes.Length);

    //        string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
    //        string header = string.Format(headerTemplate, "file", fileName, contentType);
    //        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
    //        requestStream.Write(headerbytes, 0, headerbytes.Length);

    //        requestStream.Write(fileBinData, 0, fileBinData.Length);

    //        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
    //        requestStream.Write(trailer, 0, trailer.Length);
    //        requestStream.Close();
    //        requestStream = null;

    //        WebResponse response = null;
    //        try
    //        {
    //            response = request.GetResponse();
    //            Stream responseStream = response.GetResponseStream();
    //            StreamReader responseReader = new StreamReader(responseStream);
    //        }
    //        catch (Exception ex)
    //        {
    //            //Console.WriteLine(ex.Message);
    //        }
    //        finally
    //        {
    //            if (response != null)
    //            {
    //                response.Close();
    //                response = null;
    //            }
    //            request = null;
    //        }

    //    }

    //    protected static byte[] DownloadFileInternal(string downloadFileUrl, string apiToken)
    //    {
    //        // Assign values to these objects here so that they can
    //        // be referenced in the finally block
    //        Stream remoteStream = null;
    //        Stream localStream = null;
    //        WebResponse response = null;

    //        // Use a try/catch/finally block as both the WebRequest and Stream
    //        // classes throw exceptions upon error
    //        try
    //        {
    //            // Create a request for the specified remote file name
    //            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadFileUrl);
    //            request.Method = "GET";
    //            request.Headers.Add("Authorization", apiToken);

    //            if (request != null)
    //            {
    //                // Send the request to the server and retrieve the
    //                // WebResponse object 
    //                response = request.GetResponse();
    //                if (response != null)
    //                {
    //                    // Once the WebResponse object has been retrieved,
    //                    // get the stream object associated with the response's data
    //                    remoteStream = response.GetResponseStream();

    //                    using (MemoryStream mstr = new MemoryStream())
    //                    {
    //                        remoteStream.CopyTo(mstr);
    //                        //возвращаем полученный с сервиса объект в виде массива байт 
    //                        return mstr.ToArray();
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //Console.WriteLine(e.Message);
    //        }
    //        finally
    //        {
    //            if (response != null) response.Close();
    //            if (remoteStream != null) remoteStream.Close();
    //            if (localStream != null) localStream.Close();
    //        }

    //        return null;
    //    }

    //    protected static byte[] CreateRequest(string url, string method, string apiToken, Object obj)
    //    {
    //        //Console.WriteLine("API request url: " + url);

    //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //        request.Method = method;
    //        request.Headers.Add("Authorization", apiToken);

    //        if (request.Method == "POST")
    //        {
    //            UTF8Encoding encoding = new UTF8Encoding();
    //            string jsonString = JsonConvert.SerializeObject(obj);
    //            var bytes = encoding.GetBytes(jsonString);
    //            request.ContentType = "application/json";
    //            request.ContentLength = bytes.Length;

    //            using (var newStream = request.GetRequestStream())
    //            {
    //                newStream.Write(bytes, 0, bytes.Length); newStream.Close();
    //            }
    //        }
    //        try
    //        {
    //            HttpWebResponse httpWResponse = (HttpWebResponse)request.GetResponse();
    //            using (MemoryStream mstr = new MemoryStream())
    //            {
    //                httpWResponse.GetResponseStream().CopyTo(mstr);
    //                return mstr.ToArray();
    //            }
    //        }
    //        catch (WebException ex)
    //        {
    //            var stCode = ((HttpWebResponse)ex.Response).StatusCode;
    //        }
    //        return null;
    //    }

    //    protected static string GetApiToken(string apiUrl, string apiLogin, string apiPassword)
    //    {
    //        string url = apiUrl;
    //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //        request.Method = "POST";
    //        request.ContentType = "application/json";

    //        var body = "{\"userName\":\"" + apiLogin + "\",\"password\":\"" + apiPassword + "\"}";
    //        var data = System.Text.Encoding.UTF8.GetBytes(body);

    //        request.ContentLength = data.Length;
    //        using (var stream = request.GetRequestStream())
    //        {
    //            stream.Write(data, 0, data.Length);
    //        }

    //        try
    //        {
    //            var response = (System.Net.HttpWebResponse)request.GetResponse();
    //            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

    //            Dictionary<string, Object> responseDictionary = null;

    //            if (String.IsNullOrEmpty(responseString) == false)
    //            {
    //                responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseString);

    //                if (responseDictionary.ContainsKey("statusCode") == true
    //                    && responseDictionary["statusCode"] != null
    //                    && responseDictionary["statusCode"].ToString().Equals("201") == true
    //                    && responseDictionary.ContainsKey("response") == true
    //                    && String.IsNullOrEmpty(responseDictionary["response"].ToString()) == false)
    //                {
    //                    Dictionary<string, Object> responseDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseDictionary["response"].ToString());

    //                    if (responseDataDictionary.ContainsKey("token") == true
    //                        && responseDataDictionary["token"] != null)
    //                    {
    //                        return responseDataDictionary["token"].ToString();
    //                    }
    //                }
    //            }
    //        }
    //        catch (WebException ex)
    //        {
    //            var stCode = ((HttpWebResponse)ex.Response).StatusCode;
    //        }
    //        return String.Empty;
    //    }
    //    public static bool CheckPayrollAccess(IPrincipal contextUser)
    //    {
    //        bool result = false;
    //        try
    //        {
    //            string docServerLogin = _applicationUserService.GetOOLogin();
    //            string docServerPassword = _applicationUserService.GetOOPassword();

    //            Stream employeePayrollSheetStream = null;

    //            if (String.IsNullOrEmpty(docServerLogin) == false
    //                && String.IsNullOrEmpty(docServerPassword) == false)
    //            {
    //                string employeePayrollSheetFileUrl = null;

    //                employeePayrollSheetFileUrl = _financeService.GetOODefalutCPFileUrl();

    //                if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == false)
    //                {
    //                    byte[] employeePayrollSheetFileBinData = DownloadFile(employeePayrollSheetFileUrl, docServerLogin, docServerPassword);

    //                    if (employeePayrollSheetFileBinData != null)
    //                    {
    //                        employeePayrollSheetStream = new MemoryStream(employeePayrollSheetFileBinData);
    //                    }
    //                }
    //            }

    //            if (employeePayrollSheetStream != null)
    //            {
    //                result = true;
    //            }
    //        }
    //        catch (Exception)
    //        {
    //            result = false;
    //        }

    //        return result;
    //    }
    //}
}
