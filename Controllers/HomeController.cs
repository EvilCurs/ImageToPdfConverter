using Microsoft.AspNetCore.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageToPdfConverter.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConvertToPdf(List<IFormFile> images)
    {
        try
        {
            if (images == null || images.Count == 0)
                return BadRequest(new { error = "Выберите изображения" });

            using var ms = new MemoryStream();
            var document = new Document();
            var writer = PdfWriter.GetInstance(document, ms);
            document.Open();

            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    using var imageStream = image.OpenReadStream();
                    using var img = await SixLabors.ImageSharp.Image.LoadAsync(imageStream);
                    
                    // Оптимизация размера
                    if (img.Width > 1000 || img.Height > 1000)
                    {
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(1000, 1000),
                            Mode = ResizeMode.Max
                        }));
                    }

                    using var msImg = new MemoryStream();
                    await img.SaveAsJpegAsync(msImg);
                    
                    var pdfImage = iTextSharp.text.Image.GetInstance(msImg.ToArray());
                    pdfImage.ScaleToFit(document.PageSize.Width - 40, document.PageSize.Height - 40);
                    pdfImage.Alignment = Element.ALIGN_CENTER;
                    
                    document.Add(pdfImage);
                    
                    if (images.IndexOf(image) < images.Count - 1)
                        document.NewPage();
                }
            }

            document.Close();
            writer.Close();
            
            return File(ms.ToArray(), "application/pdf", $"converted_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}