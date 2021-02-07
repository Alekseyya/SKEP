using System;
using System.IO;
using System.Net.Mail;
using Core.Config;
using Microsoft.Extensions.DependencyInjection;


namespace MainApp.Helpers
{
    public class RPCSEmailHelper
    {
        private static IServiceProvider _serviceProvider;
        private static SMTPConfig _smtpConfig;

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _smtpConfig = _serviceProvider.GetRequiredService<SMTPConfig>();
        }

        public static string GetDefaultEmailHtmlStyle()
        {
            string html = "";

            html = "<style>\r\n";
            html += " TABLE.mail { border-style:none; border-collapse:collapse; font:8pt Tahoma; width:100%; } \r\n";
            html += " TD.header { background:#d9d9d9; border:1px solid #E8EAEC; padding:2pt 10px 2pt 10px; } \r\n ";
            html += " TD.main { padding:4pt 0px 4pt 0px; } \r\n ";
            html += " TD.footer { border-width:1px; border-style:solid none none none; border-color:#9CA3AD; padding:4pt 10px 4pt 10px; } \r\n ";
            html += " DIV.title { font:10pt Arial; font:bold; padding:4pt 10px 4pt 0px; } \r\n ";
            html += " DIV.headertext { margin:5px 0px 7px 0px; } \r\n ";
            html += " DIV.error { font-weight:bold; } \r\n ";
            html += " DIV.comment { color:#9CA3AD; } \r\n";
            html += " SPAN.wfname { font:bold italic; }\r\n";
            html += " .summarycontent { margin-top:3px; margin-bottom:3px; border-bottom:none; border-top:solid 1px #000000;} \r\n ";
            html += " .summaryitemmain { font-family: tahoma; font-size:8pt; padding: 0px 10px 0px 10px; vertical-align: top;} \r\n ";
            html += " .summaryitemmain {color: #000000;} \r\n ";
            html += " .summaryitemmain { border-bottom:solid 1px #000000; border-top:none; border-left:solid 1px #000000; border-right:solid 1px #000000; padding-top:2px; padding-bottom:2px; } \r\n ";
            html += " DIV.vertext { font:8pt Arial; font:normal; } .vertablecell {width:220px; } \r\n";
            html += " .listitemcontent { margin-top:3px; margin-bottom:3px; width: 100%; border-bottom:solid 1px #9ca3ad; border-top:solid 1px #9ca3ad;} \r\n ";
            html += " .formlabel {border-right:solid 1px #e8eaec;}\r\n";
            html += " .formlabel, .formmain  { font-family: tahoma; font-size:8pt; padding: 0px 7px 0px 7px; vertical-align: top;}\r\n";
            html += " .formlabel {color: #616a76; font-weight: bold; }\r\n";
            html += " .formmain {color: #000000;}\r\n";
            html += " .formlabel, .formmain { border-bottom:solid 1px #e8eaec; padding-top:2px; padding-bottom:2px; }\r\n";
            html += " .formlabel, .formmain {background: #f8f8f9; }\r\n";
            html += "</style>";
            html += "\r\n";

            return html;
        }

        public static string GetSimpleHtmlEmailBody(string title,
            string htmlContent, string actionsHtml)
        {
            string html = RPCSEmailHelper.GetDefaultEmailHtmlStyle();


            html += @"<table cellpadding='2' cellspacing='0' class='mail' DIR='none'><tr class='header'><td class='header'><div class='title'>";
            html += "\r\n";

            html += title;
            html += "\r\n";

            html += @"</div>";
            html += "\r\n";

            if (String.IsNullOrEmpty(actionsHtml) == false)
            {
                html += @"<div class='headertext'>";
                html += "\r\n";
                html += actionsHtml;

                html += @"</div>";
                html += "\r\n";
            }

            html += @"</td></tr><tr class='main'><td class='main' valign='top'><div>";
            html += "\r\n";

            html += htmlContent;
            html += "\r\n";

            html += @"</div></td></tr>";
            html += "\r\n";

            return html;
        }

        public static bool SendHtmlEmailViaSMTP(string to,
            string subject,
            string from,
            string replyTo,
            string bodyHtml,
            string cc,
            string bcc)
        {
            return SendHtmlEmailViaSMTP(to,
                subject,
                from,
                replyTo,
                bodyHtml,
                cc,
                bcc,
                null, null);
        }

        public static bool SendHtmlEmailViaSMTP(string to,
            string subject,
            string from,
            string replyTo,
            string bodyHtml,
            string cc,
            string bcc,
            Stream attachmentFileBinaryStream,
            string attachmentFileName)
        {
            bool result = false;

            try
            {
                if (String.IsNullOrEmpty(_smtpConfig.Server) == false)
                {
                    string smtpServer =  _smtpConfig.Server;
                    using (SmtpClient smtpClient = new SmtpClient(smtpServer))
                    {
                        if (String.IsNullOrEmpty(_smtpConfig.Login) == false
                            && String.IsNullOrEmpty(_smtpConfig.Password) == false)
                        {
                            smtpClient.Credentials = new System.Net.NetworkCredential(_smtpConfig.Login, _smtpConfig.Password);
                        }

                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Port = 25;
                        smtpClient.EnableSsl = false;//true;

                        MailMessage message = new MailMessage();

                        message.From = new MailAddress(_smtpConfig.FromEmail);

                        if (String.IsNullOrEmpty(from) == false)
                        {
                            message.From = new MailAddress(from);
                        }

                        string[] toAddresses = to.Split(';');
                        foreach (string address in toAddresses)
                        {
                            message.To.Add(new MailAddress(address));
                        }

                        if (String.IsNullOrEmpty(replyTo) == false)
                        {
                            message.ReplyTo = new MailAddress(replyTo);
                        }

                        if (String.IsNullOrEmpty(cc) == false)
                        {
                            string[] ccAddresses = cc.Split(';');
                            foreach (string address in ccAddresses)
                            {
                                message.CC.Add(new MailAddress(address));
                            }
                        }

                        if (String.IsNullOrEmpty(bcc) == false)
                        {
                            string[] bccAddresses = bcc.Split(';');
                            foreach (string address in bccAddresses)
                            {
                                message.Bcc.Add(new MailAddress(address));
                            }
                        }

                        message.IsBodyHtml = true;
                        message.Body = bodyHtml;
                        message.Subject = subject;

                        if (attachmentFileBinaryStream != null
                            && String.IsNullOrEmpty(attachmentFileName) == false)
                        {
                            message.Attachments.Add(new Attachment(attachmentFileBinaryStream, attachmentFileName));
                        }

                        try
                        {
                            smtpClient.Send(message);

                            result = true;
                        }
                        catch (Exception ex)
                        {
                            result = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }


    }
}