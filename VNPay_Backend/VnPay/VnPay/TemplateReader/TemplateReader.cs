namespace VnPay.TemplateReader
{
    public class TemplateReader : ITemplateReader
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TemplateReader(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> GetTemplate(string templateName)
        {
            string templateEmail = Path.Combine(_webHostEnvironment.ContentRootPath, templateName);

            string content = await File.ReadAllTextAsync(templateEmail);

            return content;
        }
    }
}
