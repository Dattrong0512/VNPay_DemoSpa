namespace VnPay.TemplateReader
{
    public interface ITemplateReader
    {
        Task<string> GetTemplate(string templateName);
    }
}
