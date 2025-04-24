using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ImageMagick;
using System.Text;
using Tesseract;
using Microsoft.VisualBasic;





namespace OcrFunctionApp;

public class Functions
{
    private readonly ILogger _logger;

    public Functions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
    }

    [Function("UploadPdf")]
    public async Task<HttpResponseData> UploadPdf(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("function triggered...");
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string user_name = query["name"]?? "stranger";
        var parser =await MultipartFormDataParser.ParseAsync(req.Body);
        var file  = parser.Files.FirstOrDefault();
        if(file == null)
        {
            _logger.LogInformation("no file parsed!");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("No file was uploaded");
            return badResponse;
        }

        // saving the file 
        string fileName = file.FileName;
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
        var uploadDir = Path.Combine(projectRoot, "UploadedFiles");
        Directory.CreateDirectory(uploadDir);
        string filePath = Path.Combine(uploadDir, fileName);
        using(var stream = File.Create(filePath))
        {
            await file.Data.CopyToAsync(stream);
        }

        // preparing paths for OCR and image converting
        var tessPath = Path.Combine(projectRoot, "tessdata");
        var exists = Directory.Exists(tessPath);
        _logger.LogInformation($"Tessdata path: {tessPath}, Exists: {exists}");
        string imagesDir = Path.Combine(projectRoot, "TempImages");
        ConvertToImages(filePath, imagesDir);
        string extractedText = ExtractText(imagesDir,tessPath);
        _logger.LogInformation($"Exists? {File.Exists(Path.Combine(tessPath, "eng.traineddata"))}");
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            fileName,
            text = extractedText,
            message = $"Hello {user_name} the PDF named {fileName} is received successfully!",

        });
        return response;


        

    }

    public static void ConvertToImages(string pdfPath, string outputDir)
    {
        Directory.CreateDirectory(outputDir); 

        var settings = new MagickReadSettings
        {
            Density = new Density(300, 300) 
        };

        using (var images = new MagickImageCollection())
            {
                images.Read(pdfPath, settings);

                int pageIndex = 0;
                foreach (var image in images)
                {
                    image.Format = MagickFormat.Png;
                    string outputPath = Path.Combine(outputDir, $"page_{pageIndex}.png");
                    image.Write(outputPath);
                    pageIndex++;
                }
            }
    }
    
    public static string ExtractText(string imageFolderPath, string tesspath)
    {
        var textBuilder = new StringBuilder();
        var images = Directory.GetFiles(imageFolderPath,"*.png");

        // assumes the english language
        using(var engine = new TesseractEngine(tesspath, "eng", EngineMode.Default))
        {
            foreach (var imagePath in images.OrderBy(p => p)) 
            {
                using (var img = Pix.LoadFromFile(imagePath))
                using (var page = engine.Process(img))
                {
                    textBuilder.AppendLine(page.GetText());
                }
            }

        }
        return textBuilder.ToString();
    }
}