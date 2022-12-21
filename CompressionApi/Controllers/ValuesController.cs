using Microsoft.AspNetCore.Mvc;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf;
using System.Security.Claims;
using MFilesAPI;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CompressionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ValuesController> _logger;
        public ValuesController(IConfiguration Configuration, ILogger<ValuesController> logger)
        {
            _configuration = Configuration ?? throw new ArgumentNullException(nameof(Configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            string Username = this._configuration.GetConnectionString($"Username");
            string Password = this._configuration.GetConnectionString($"Password");
            string domain = this._configuration.GetConnectionString($"Domain");

            // Instantiate an MFilesServerApplication object.
            // https://www.m-files.com/api/documentation/MFilesAPI~MFilesServerApplication.html
            var mfServerApplication = new MFilesServerApplication();

            // Connect to a local server using the default parameters (TCP/IP, localhost, current Windows user).
            // https://www.m-files.com/api/documentation/index.html#MFilesAPI~MFilesServerApplication~Connect.html
            mfServerApplication.Connect(MFAuthType.MFAuthTypeSpecificWindowsUser,
                UserName: Username,
                Password: Password,
                Domain: domain,
                ProtocolSequence: "ncacn_ip_tcp", // Connect using TCP/IP.
                NetworkAddress: "localhost", // Connect to m-files.mycompany.com
                Endpoint: "2266");

            // Obtain a connection to the vault with GUID {C840BE1A-5B47-4AC0-8EF7-835C166C8E24}.
            // Note: this will except if the vault is not found.
            try
            {
                var vault = mfServerApplication.LogInToVault("{1E523561-FFC4-467C-9102-9411B855F657}");
                // Create the condition.
                var searchCondition = new SearchConditions();
                //class
                {
                    var condition = new SearchCondition();
                    // Create an array of the class Ids.
                    // Matched objects must have one of these class Ids.
                    var classIds = new[] {119};


                    // We want to search by property - in this case the built-in "class" property.
                    // Alternatively we could pass the ID of the property definition if it's not built-in.
                    condition.Expression.SetPropertyValueExpression(
                        (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                                    MFParentChildBehavior.MFParentChildBehaviorNone);

                    // We want only items that equal one of the class Ids.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // We want to search for items whose class property is one of the supplied class Ids.
                    // This should be MFDatatypeMultiSelectLookup, even though the property is MFDatatypeLookup.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeMultiSelectLookup, classIds);
                    searchCondition.Add(-1, condition);
                }

                //extension

                {
                    var condition = new SearchCondition();
                    // Set the expression.
                    condition.Expression.SetFileValueExpression(MFFileValueType.MFFileValueTypeFileName);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeContains;

                    // Set the value.
                    condition.TypedValue.SetValue(MFilesAPI.MFDataType.MFDatatypeText, ".pdf");

                    searchCondition.Add(-1, condition);
                }
                //deleted
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);

                    searchCondition.Add(-1, condition);
                }
                //searching with display id
                {
                    // Create the condition.
                    var condition = new SearchCondition();

                    // Set the expression.
                    condition.Expression.DataStatusValueType = MFStatusType.MFStatusTypeExtID;

                    // Set the condition type.
                    condition.ConditionType = MFConditionType.MFConditionTypeEqual;

                    // Set the value.
                    // In this case "MyExternalObjectId" is the ID of the object in the remote system.
                    condition.TypedValue.SetValue(MFDataType.MFDatatypeText, id.ToString());

                    searchCondition.Add(-1, condition);

                }
                var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(searchCondition, MFSearchFlags.MFSearchFlagNone, SortResults: false);
                
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

                string subPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

                bool exists = System.IO.Directory.Exists(subPath);

                string filepath = "";
                string filepath1 = "";

                if (!exists)
                    System.IO.Directory.CreateDirectory(subPath);

                string folder = Path.Combine(Directory.GetCurrentDirectory(), "Files");

                if (searchResults.Count > 0)
                {
                    foreach (ObjectVersion objectVersion in searchResults)
                    {
                        foreach (ObjectFile file in objectVersion.Files)
                        {
                            filepath = System.IO.Path.Combine(subPath, $"{id}.{file.Extension}");
                            filepath1  = System.IO.Path.Combine(subPath, $"{id}_.{file.Extension}");
                            vault.ObjectFileOperations.DownloadFile(file.ID, file.Version,filepath );

                            //Load an existing PDF
                            FileStream docStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                            FileStream docStream1 = new FileStream(filepath1, FileMode.OpenOrCreate, FileAccess.ReadWrite);


                            //Load the existing PDF document
                            PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);

                            //Create a new compression option.
                            PdfCompressionOptions options = new PdfCompressionOptions();

                            //Enable the compress image.
                            options.CompressImages = true;

                            //Set the image quality.
                            options.ImageQuality = 10;
                            options.OptimizeFont = true;
                            options.OptimizePageContents = true;
                            options.RemoveMetadata = true;

                            //Assign the compression option to the document
                            loadedDocument.Compress(options);
                            //Save the document into stream.
                            loadedDocument.Save(docStream1);

                            //Close the documents.
                            loadedDocument.Close(true);
                            docStream.Close();
                            docStream1.Close();
                            docStream.Dispose();
                            docStream1.Dispose();

                        }

                        var objID = new MFilesAPI.ObjID();
                        objID.SetIDs(
                        ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument,
                        ID: objectVersion.ObjVer.ID);

                        // Check out the object.
                        var checkedOutObjectVersion = vault.ObjectOperations.CheckOut(objID);


                        foreach (ObjectFile objectFile1 in checkedOutObjectVersion.Files)
                        {
                            vault.ObjectFileOperations.UploadFile(objectFile1.ID, objectFile1.Version, filepath1); //Setting Single File of an Object to the new file 
                        }
                        // vault.ObjectFileOperations.AddFile(
                        //ObjVer: checkedOutObjectVersion.ObjVer,
                        //Title: "My test document",
                        //Extension: "pdf",
                        //SourcePath: @"D:\FinalTestSined1.pdf");

                        // Check the object back in.
                        vault.ObjectOperations.CheckIn(checkedOutObjectVersion.ObjVer);
                    }


                    try
                    {
                        deletefile(filepath);
                        deletefile(filepath1);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex);
                    }

                }
                return Ok($"Object ID:{id} successfully Compressed");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }
        private void deletefile(string filepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                try
                {
                    System.IO.File.Delete(filepath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The deletion failed: {0}", e.Message);
                }
            }
            else
            {
                Console.WriteLine("Specified file doesn't exist");
            }
        }
    }

}
