using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Storfiler.AspNetCore.Extensions
{
    public static class HttpContextExtensions
    {
        private static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(new JsonSerializerSettings { 
            NullValueHandling = NullValueHandling.Ignore, 
            DefaultValueHandling = DefaultValueHandling.Ignore, 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, 
            ContractResolver = new CamelCasePropertyNamesContractResolver() 
        }); 
         
        public static Task WriteJsonAsync(this HttpContext httpContext, object value)
        {           
            httpContext.Response.ContentType = "application/json";
             
            using (var writer = new HttpResponseStreamWriter(httpContext.Response.Body, Encoding.UTF8)) 
            { 
                using (var jsonWriter = new JsonTextWriter(writer) { CloseOutput = false, AutoCompleteOnClose = false }) 
                { 
                    JsonSerializer.Serialize(jsonWriter, value); 
                } 
                return writer.FlushAsync(); 
            } 
        } 
  
        public static T ReadJsonBody<T>(this HttpContext httpContext) 
        {
            using (var streamReader = new StreamReader(httpContext.Request.Body)) 
            { 
                using (var jsonTextReader = new JsonTextReader(streamReader) { CloseInput = false }) 
                { 
                    var model = JsonSerializer.Deserialize<T>(jsonTextReader);

                    return model;
                } 
            }
        } 
    }
}