using MailKit.Net.Smtp;
using MimeKit;


namespace CVision.Services.MailService
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendNotificationEmail()
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("CvReview App", smtpSettings["FromEmail"]));
            emailMessage.To.Add(new MailboxAddress("", smtpSettings["ToEmail"]));
            emailMessage.Subject = "Yeni Cv Review İsteği Alındı!";
            emailMessage.Body = new TextPart("plain")
            {
                Text = "Bir kullanıcı özgeçmiş inceleme isteği gönderdi. Bu, kullanıcı sayınızı takip etmenize yardımcı olabilir."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpSettings["SmtpServer"], int.Parse(smtpSettings["SmtpPort"]), false);
                await client.AuthenticateAsync(smtpSettings["SmtpUser"], smtpSettings["SmtpPassword"]);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
