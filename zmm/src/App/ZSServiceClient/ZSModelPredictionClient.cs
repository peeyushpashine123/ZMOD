using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ZMM.Helpers.Common;

namespace ZMM.App.ZSServiceClient
{
    public class ZSModelPredictionClient : IZSModelPredictionClient
    {
        public IConfiguration configuration { get; }
        public ZSModelPredictionClient(IConfiguration _configuration)
        {
            this.configuration = _configuration;
        }

        #region GetModels - [GET] http://dcindgo01:8083/adapars/models
        public async Task<string> GetModels()
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));

            using(var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = auth; 
               
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync("models");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        jsonResult = await response.Content.ReadAsStringAsync();
                    } 
                    else
                        return ZMMConstants.ErrorFailed;
                }
                catch(HttpRequestException ex)
                {
                    jsonResult = "{'message': '" + ex.Message + "', 'error':'"+ ZMMConstants.ErrorFailed +"'}";
                }            
            }

            return jsonResult;
        }
        #endregion
    
        #region Delete Pmml [DELETE] http://dcindgo01:8083/adapars/models/{modelName}
        public async Task<string> DeletePmml(string modelName)
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));

            using(var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = auth;    
                //
                var content = new FormUrlEncodedContent(
                    new List<KeyValuePair<string, string>>
                    {
                        //nothing
                    }
                );  
                //
                try
                {
                    HttpResponseMessage response = await httpClient.DeleteAsync("models/" + modelName);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        jsonResult = await response.Content.ReadAsStringAsync();
                    } 
                }
                catch(HttpRequestException ex)
                {
                    jsonResult = "{'message': '" + ex.Message + "'}";
                }            
            }

            return jsonResult;
        }


        #endregion

        #region Upload Pmml [POST]
        public async Task<string> UploadPmml(string filePath)
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Authorization = auth;
                    httpClient.Timeout=TimeSpan.FromMinutes(30);

                    using (var content = new MultipartFormDataContent("Upload--" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    {
                        var memory = new MemoryStream();
                        using (var stream = new FileStream(filePath, FileMode.Open))
                        {
                            await stream.CopyToAsync(memory);
                        }
                        memory.Position = 0;
                        content.Add(new StreamContent(memory), "file", filePath);
                        HttpResponseMessage response = await httpClient.PostAsync("models", content);
                        if (response.IsSuccessStatusCode)
                        {
                            jsonResult = await response.Content.ReadAsStringAsync();
                        }
                        else if(response.StatusCode.ToString() == "402")
                        {
                            return "FileExists";
                        }
                        else
                        {
                            return "Fail";
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                string _ex=ex.Message;
                return "fail";
            }
            

            return jsonResult;
        }
        #endregion
    
        #region single scoring [GET] /apply/SVM_Model?record={}
        //http://dcindgo01:8083/adapars/apply/SVM_Model?record={"sepal length (cm)":5,"sepal width (cm)":3,"petal length (cm)":1,"petal width (cm)":0}
        public async Task<string> SingleScoring(string modelName, string record)
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));

            using(var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = auth;    
                //                                
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync($"apply/{modelName}?record={record}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        jsonResult = await response.Content.ReadAsStringAsync();                        
                    } 
                }
                catch(HttpRequestException ex)
                {
                    //add log                    
                    jsonResult = "{'message': '" + ex.Message + "'}";
                }            
            }

            return jsonResult;
        }
        #endregion

        #region multiple score
        public async Task<string> MultipleScoring(string modelName, string filePath)
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = auth;
                //                                
                try
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        var memory = new MemoryStream();
                        using (var stream = new FileStream(filePath, FileMode.Open))
                        {
                            await stream.CopyToAsync(memory);
                        }
                        memory.Position = 0;
                        content.Add(new StreamContent(memory), "file", filePath);
                        HttpResponseMessage response = await httpClient.PostAsync($"apply/{modelName}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            jsonResult = await response.Content.ReadAsStringAsync();
                        }
                    }

                }
                catch (HttpRequestException ex)
                {
                    //add log                    
                    jsonResult = "{'message': '" + ex.Message + "'}";
                }
            }

            return jsonResult;
        }
        #endregion

        #region Image score
        public async Task<string> ImageScoring(string modelName,string filePath)
        {
            string jsonResult = string.Empty;
            var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["ZS:Username"]}:{configuration["ZS:Password"]}")));

            using(var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new System.Uri(configuration["ZS:srvurl"]);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = auth;    
                // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("eddie.soong@softwareag.com", "softwareag");   
                //                                
                try
                {
                   using (var content = new MultipartFormDataContent())
                    {
                        var memory = new MemoryStream();
                        using (var stream = new FileStream(filePath, FileMode.Open))
                        {
                            await stream.CopyToAsync(memory);
                        }
                        memory.Position = 0;
                        content.Add(new StreamContent(memory), "file", filePath);
                        HttpResponseMessage response = await httpClient.PostAsync($"apply/{modelName}",content);
                        if (response.IsSuccessStatusCode)
                        {
                            jsonResult = await response.Content.ReadAsStringAsync();                        
                        } 
                        }

                    } 
                catch(HttpRequestException ex)
                {
                    //add log                    
                    jsonResult = "{'message': '" + ex.Message + "'}";
                }            
            }
            return jsonResult;
        }
        #endregion
    }
}