using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace code
{
    public static class RunIngest
    {
        [FunctionName("RunIngest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            runDetails run;
            
            if(req.Form.Files.Count==1){
                var inputFile = req.Form.Files[0];
                if(inputFile.Length==0){
                    log.LogInformation("File is empty");
                    return new BadRequestObjectResult("File is empty");
                }

                XmlDocument xmlDoc;

                using(FileStream fs= new FileStream(inputFile.FileName, FileMode.Create)){
                    await inputFile.CopyToAsync(fs);
                    StreamReader reader = new StreamReader(fs);
                    fs.Position=0;
                    xmlDoc=new XmlDocument();
                    xmlDoc.LoadXml(reader.ReadToEnd());
                }

                var jsonDoc = JsonConvert.SerializeXmlNode(xmlDoc);
                dynamic myJObject = JObject.Parse(jsonDoc);

                run = new runDetails();
                run.id=System.Guid.NewGuid().ToString();
                run.date=myJObject.TrainingCenterDatabase.Activities.Activity.Id;

                foreach(var lap in myJObject.TrainingCenterDatabase.Activities.Activity.Lap){
                    run.meters += Convert.ToDouble(lap.DistanceMeters);
                }

                run.miles = 0.000621371*run.meters;

            }
            else{
                log.LogInformation("Need to send one file");
                return new BadRequestObjectResult("Need to send one file");
            }
            


            string responseMessage = JsonConvert.SerializeObject(run, Newtonsoft.Json.Formatting.Indented);

            return new OkObjectResult(responseMessage);
        }
    }

    public class runDetails
    {
        public string id { get; set; }
        public DateTime date { get; set; }
        public double meters { get; set; }
        public double miles { get; set; }

    }

}
