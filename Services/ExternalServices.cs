using CafeOrdering.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Http.Json;
namespace CafeOrdering.Services;
public interface IEmailService { Task SendVerificationAsync(string email,string customer,string link); }
public class EmailService(IConfiguration c, ILogger<EmailService> log) : IEmailService {
 public async Task SendVerificationAsync(string email,string customer,string link) { var user=c["Email:User"]; var pass=c["Email:AppPassword"]; if(string.IsNullOrWhiteSpace(user)||string.IsNullOrWhiteSpace(pass)) { log.LogWarning("Email settings missing. Verification link: {Link}",link); return; }
  var m=new MimeMessage(); m.From.Add(new MailboxAddress(c["Email:FromName"]??"Cafe",c["Email:FromEmail"]??user)); m.To.Add(MailboxAddress.Parse(email)); m.Subject="تأكيد طلبك من المقهى"; m.Body=new BodyBuilder{HtmlBody=$"<div dir='rtl' style='font-family:Arial'><h2>مرحباً {System.Net.WebUtility.HtmlEncode(customer)}</h2><p>اضغط لتأكيد طلبك:</p><a href='{link}'>تأكيد الطلب</a><p>الرابط صالح لمدة 30 دقيقة.</p></div>"}.ToMessageBody(); using var smtp=new SmtpClient(); await smtp.ConnectAsync(c["Email:Host"]??"smtp.gmail.com",int.Parse(c["Email:Port"]??"587"),SecureSocketOptions.StartTls); await smtp.AuthenticateAsync(user,pass); await smtp.SendAsync(m); await smtp.DisconnectAsync(true);
 }
}
public interface ITelegramService { Task SendOrderAsync(Order order); }
public class TelegramService(HttpClient http,IConfiguration c,ILogger<TelegramService> log):ITelegramService {
 public async Task SendOrderAsync(Order o) { var token=c["Telegram:BotToken"]; var chat=c["Telegram:ChatId"]; if(string.IsNullOrWhiteSpace(token)||token.StartsWith("YOUR_")||string.IsNullOrWhiteSpace(chat)) { log.LogWarning("Telegram is not configured; order {Order} was not sent.",o.OrderNumber); return; }
  var lines=o.Items.Select(i=>$"• {i.ProductName} × {i.Quantity} = {(i.UnitPrice*i.Quantity):0.00}"); var msg=$"☕ طلب جديد #{o.OrderNumber}\nالاسم: {o.CustomerName}\nالهاتف: {o.Phone}\nالبريد: {o.Email}\nالموقع: {o.Location}\nالتفاصيل: {o.LocationDetails}\n\n{string.Join("\n",lines)}\n\nالإجمالي: {o.Total:0.00}";
  var r=await http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/sendMessage",new{chat_id=chat,text=msg}); r.EnsureSuccessStatusCode();
 }
}
public interface IPdfService { byte[] CreateMenu(IEnumerable<Product> products); }

public class PdfService : IPdfService
{
    public byte[] CreateMenu(IEnumerable<Product> products)
    {
        return Document.Create(c => c.Page(p =>
        {
            p.Margin(35);

            // تحديد الخط العربي لجميع نصوص الصفحة
            p.DefaultTextStyle(x => x
                .FontFamily("Cairo")
                .FontSize(12)
            );

            p.Content().Column(col =>
            {
                col.Item()
                    .AlignCenter()
                    .Text("قائمة المقهى")
                    .FontSize(24)
                    .Bold();

                col.Item()
                    .PaddingVertical(12)
                    .LineHorizontal(1);

                foreach (var x in products)
                {
                    col.Item()
                        .PaddingVertical(7)
                        .Row(r =>
                        {
                            r.RelativeItem()
                                .Text(x.Name)
                                .FontSize(14)
                                .Bold();

                            r.ConstantItem(100)
                                .AlignLeft()
                                .Text($"{x.Price:0.00}");
                        });

                    col.Item()
                        .Text(x.Description ?? "")
                        .FontColor(Colors.Grey.Darken1)
                        .FontSize(10);
                }
            });

            p.Footer()
                .AlignCenter()
                .Text("شكراً لزيارتكم");
        })).GeneratePdf();
    }
}
