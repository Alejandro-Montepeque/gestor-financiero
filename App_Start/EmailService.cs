using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace gestor_financiero
{
    /// <summary>
    /// Envia correos via SMTP usando las credenciales configuradas en Web.config:
    ///   Smtp:Host, Smtp:Port, Smtp:User, Smtp:Password, Smtp:From, Smtp:FromName
    ///
    /// Para Gmail necesitas:
    ///   1. Activar 2FA en la cuenta de Google
    ///   2. Generar una "App Password" (Contrasena de aplicacion) en
    ///      https://myaccount.google.com/apppasswords
    ///   3. Usar esa contrasena de 16 caracteres en Smtp:Password
    ///
    ///   Host: smtp.gmail.com
    ///   Port: 587
    ///   EnableSsl: true  (TLS)
    /// </summary>
    public class EmailService
    {
        private readonly string host;
        private readonly int port;
        private readonly string user;
        private readonly string password;
        private readonly string fromAddress;
        private readonly string fromName;

        public EmailService()
        {
            host        = ConfigurationManager.AppSettings["Smtp:Host"]     ?? "smtp.gmail.com";
            int.TryParse(ConfigurationManager.AppSettings["Smtp:Port"], out port);
            if (port == 0) port = 587;
            user        = ConfigurationManager.AppSettings["Smtp:User"]     ?? "";
            password    = ConfigurationManager.AppSettings["Smtp:Password"] ?? "";
            fromAddress = ConfigurationManager.AppSettings["Smtp:From"]     ?? user;
            fromName    = ConfigurationManager.AppSettings["Smtp:FromName"] ?? "Gestor Financiero";
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "SMTP no configurado. Setea Smtp:User y Smtp:Password en Web.config.");

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(fromAddress, fromName);
                msg.To.Add(to);
                msg.Subject = subject;
                msg.Body = htmlBody;
                msg.IsBodyHtml = true;

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(user, password);
                    client.Timeout = 20000; // 20 segundos

                    await client.SendMailAsync(msg);
                }
            }
        }

        /// <summary>
        /// Plantilla generica de OTP. Recibe el codigo y un mensaje
        /// contextual (registro vs reset). El usuario ve el codigo grande
        /// y centrado para copiarlo facilmente.
        /// </summary>
        private static string BuildOtpHtml(string nombre, string codigo, string titulo, string contexto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /></head>
<body style='margin:0;padding:0;background:#f8fafc;font-family:Arial,Helvetica,sans-serif;color:#1e293b;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f8fafc;padding:24px 0;'>
    <tr>
      <td align='center'>
        <table width='560' cellpadding='0' cellspacing='0' style='background:white;border-radius:12px;box-shadow:0 4px 20px rgba(0,0,0,0.06);overflow:hidden;'>
          <tr>
            <td style='background:linear-gradient(135deg,#4f46e5,#4338ca);padding:24px;color:white;text-align:center;'>
              <div style='display:inline-block;background:white;color:#4f46e5;width:48px;height:48px;border-radius:12px;font-weight:bold;font-size:24px;line-height:48px;'>$</div>
              <h1 style='margin:12px 0 0;font-size:22px;'>Gestor Financiero</h1>
            </td>
          </tr>
          <tr>
            <td style='padding:32px;'>
              <h2 style='margin:0 0 16px;font-size:20px;color:#1e293b;'>Hola {nombre},</h2>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.5;color:#475569;'>{contexto}</p>

              <p style='margin:0 0 8px;font-size:12px;color:#64748b;text-align:center;text-transform:uppercase;letter-spacing:1px;'>{titulo}</p>
              <div style='margin:0 auto 24px;background:#eef2ff;border:2px dashed #4f46e5;border-radius:12px;padding:20px;text-align:center;'>
                <div style='font-family:''Courier New'',monospace;font-size:38px;font-weight:bold;color:#4338ca;letter-spacing:8px;'>{codigo}</div>
              </div>
              <p style='margin:0 0 8px;font-size:13px;color:#64748b;text-align:center;'>Este codigo expira en <strong>10 minutos</strong>.</p>

              <hr style='border:0;border-top:1px solid #e2e8f0;margin:24px 0;'/>
              <p style='margin:0;font-size:12px;color:#94a3b8;'>
                Si no solicitaste este codigo, ignora este correo. Nadie podra usarlo sin tu contrasena.
              </p>
            </td>
          </tr>
          <tr>
            <td style='background:#f1f5f9;padding:16px 24px;font-size:12px;color:#64748b;text-align:center;'>
              Este es un correo automatico, por favor no respondas.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        public static string BuildOtpRegistroHtml(string nombre, string codigo)
        {
            return BuildOtpHtml(
                nombre, codigo,
                "Codigo de verificacion",
                "Bienvenido a Gestor Financiero. Para terminar de crear tu cuenta, ingresa este codigo en la pantalla de verificacion:"
            );
        }

        public static string BuildOtpResetHtml(string nombre, string codigo)
        {
            return BuildOtpHtml(
                nombre, codigo,
                "Codigo para restablecer contrasena",
                "Recibimos una solicitud para restablecer tu contrasena. Ingresa este codigo en la pantalla de verificacion para continuar:"
            );
        }
    }
}
