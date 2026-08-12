namespace GestorFinanciero.Infrastructure.Email;

/// <summary>
/// Hand-crafted HTML templates for the transactional emails the app sends.
/// Kept inline (as opposed to Razor files) so the Infrastructure layer stays
/// self-contained and doesn't need a view engine.
/// </summary>
public static class EmailTemplates
{
    private const string BrandGradient =
        "background:linear-gradient(90deg,#A78BFA,#22D3EE,#F472B6);" +
        "-webkit-background-clip:text;background-clip:text;color:transparent;";

    public static string Welcome(string fullName) => Layout(
        title: "¡Bienvenido a Gestor Financiero!",
        body: $"""
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>Hola <strong>{Escape(fullName)}</strong>,</p>
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>
                Bienvenido a <strong>Gestor Financiero</strong>. Tu cuenta ya está lista
                y arrancamos con 5 categorías por defecto (Salario, Alquiler, Alimentos,
                Ahorros y Otros) para que empieces a registrar movimientos ahora mismo.
            </p>
            <div style='margin:32px 0;text-align:center;'>
                <a href='https://gestor.alejandromontepeque.dev'
                   style='display:inline-block;padding:12px 28px;
                          background:#7C3AED;color:#fff;text-decoration:none;
                          border-radius:8px;font-weight:600;'>
                    Ir al panel
                </a>
            </div>
            <p style='font-size:14px;color:#6b7280;line-height:1.6;margin:0;'>
                Consejo: cargá tu primer ingreso del mes y empezá a ver el panel
                con datos reales — comparativas, distribución de gastos y tasa
                de ahorro se activan automáticamente.
            </p>
            """);

    public static string ConfirmEmail(string fullName, string confirmLink) => Layout(
        title: "Confirmá tu email",
        body: $"""
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>Hola <strong>{Escape(fullName)}</strong>,</p>
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>
                Casi listo — solo falta que confirmes tu correo electrónico para
                activar la cuenta y poder iniciar sesión.
            </p>
            <div style='margin:32px 0;text-align:center;'>
                <a href='{Escape(confirmLink)}'
                   style='display:inline-block;padding:12px 28px;
                          background:#7C3AED;color:#fff;text-decoration:none;
                          border-radius:8px;font-weight:600;'>
                    Confirmar email
                </a>
            </div>
            <p style='font-size:14px;color:#6b7280;line-height:1.6;margin:0;'>
                Si no fuiste vos, ignorá este mensaje. Nadie puede usar tu cuenta
                hasta que confirmes esta dirección.
            </p>
            """);

    public static string ChangeEmail(string fullName, string newEmail, string confirmLink) => Layout(
        title: "Confirmá tu nuevo email",
        body: $"""
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>Hola <strong>{Escape(fullName)}</strong>,</p>
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>
                Pediste cambiar el correo de tu cuenta a
                <strong>{Escape(newEmail)}</strong>. Confirmá el cambio con el
                botón de abajo — el correo anterior seguirá funcionando hasta
                que hagas click.
            </p>
            <div style='margin:32px 0;text-align:center;'>
                <a href='{Escape(confirmLink)}'
                   style='display:inline-block;padding:12px 28px;
                          background:#7C3AED;color:#fff;text-decoration:none;
                          border-radius:8px;font-weight:600;'>
                    Confirmar cambio de correo
                </a>
            </div>
            <p style='font-size:14px;color:#6b7280;line-height:1.6;margin:0;'>
                Si no fuiste vos, ignorá este mensaje. Nadie puede cambiar tu
                correo sin acceso a este link.
            </p>
            """);

    // ── Security notifications (fire-and-forget from Identity flows) ──

    public static string PasswordChanged(string fullName, string ipAddress, string userAgent, DateTime whenUtc) => Layout(
        title: "Tu contraseña fue cambiada",
        body: SecurityNotice(
            greeting:  $"Hola <strong>{Escape(fullName)}</strong>,",
            main:      "Detectamos un <strong>cambio de contraseña</strong> en tu cuenta de Gestor Financiero. Si fuiste vos, podés ignorar este mensaje.",
            actionCta: "Si NO fuiste vos, restablecé la contraseña ya mismo:",
            actionUrl: "https://gestor.alejandromontepeque.dev/Account/ForgotPassword",
            actionText:"Restablecer contraseña",
            ipAddress: ipAddress,
            userAgent: userAgent,
            whenUtc:   whenUtc));

    public static string EmailChanged(string fullName, string previousEmail, string newEmail, string ipAddress, string userAgent, DateTime whenUtc) => Layout(
        title: "Tu correo fue cambiado",
        body: SecurityNotice(
            greeting:  $"Hola <strong>{Escape(fullName)}</strong>,",
            main:      $"El correo de tu cuenta cambió de <strong>{Escape(previousEmail)}</strong> a <strong>{Escape(newEmail)}</strong>. Si fuiste vos, podés ignorar este mensaje.",
            actionCta: "Si NO fuiste vos, restablecé la contraseña y contactanos:",
            actionUrl: "https://gestor.alejandromontepeque.dev/Account/ForgotPassword",
            actionText:"Restablecer contraseña",
            ipAddress: ipAddress,
            userAgent: userAgent,
            whenUtc:   whenUtc));

    public static string AccountLocked(string fullName, string ipAddress, string userAgent, DateTime whenUtc, TimeSpan lockoutDuration) => Layout(
        title: "Cuenta bloqueada temporalmente",
        body: SecurityNotice(
            greeting:  $"Hola <strong>{Escape(fullName)}</strong>,",
            main:      $"Detectamos múltiples intentos fallidos de inicio de sesión en tu cuenta. Como medida de seguridad, <strong>bloqueamos temporalmente el acceso por {(int)lockoutDuration.TotalMinutes} minutos</strong>. Si fuiste vos y olvidaste la contraseña, podés restablecerla.",
            actionCta: "Restablecer contraseña:",
            actionUrl: "https://gestor.alejandromontepeque.dev/Account/ForgotPassword",
            actionText:"Restablecer contraseña",
            ipAddress: ipAddress,
            userAgent: userAgent,
            whenUtc:   whenUtc));

    private static string SecurityNotice(string greeting, string main, string actionCta, string actionUrl, string actionText, string ipAddress, string userAgent, DateTime whenUtc) => $"""
        <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>{greeting}</p>
        <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>{main}</p>
        <table role='presentation' width='100%' style='background:rgba(255,255,255,.03);border:1px solid rgba(255,255,255,.08);border-radius:8px;margin:16px 0;'>
            <tr><td style='padding:12px 16px;font-size:13px;color:#9ca3af;'>
                <div><strong style='color:#e5e7eb;'>Cuándo:</strong> {Escape(whenUtc.ToString("dd MMM yyyy — HH:mm 'UTC'"))}</div>
                <div style='margin-top:4px;'><strong style='color:#e5e7eb;'>Desde:</strong> {Escape(ipAddress)}</div>
                <div style='margin-top:4px;'><strong style='color:#e5e7eb;'>Dispositivo:</strong> {Escape(TruncateAgent(userAgent))}</div>
            </td></tr>
        </table>
        <p style='font-size:15px;line-height:1.6;margin:0 0 16px 0;color:#e5e7eb;'>{actionCta}</p>
        <div style='margin:16px 0 8px 0;text-align:center;'>
            <a href='{Escape(actionUrl)}'
               style='display:inline-block;padding:12px 28px;
                      background:#7C3AED;color:#fff;text-decoration:none;
                      border-radius:8px;font-weight:600;'>
                {Escape(actionText)}
            </a>
        </div>
        """;

    private static string TruncateAgent(string userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? "—"
        : userAgent.Length > 120 ? userAgent[..117] + "…" : userAgent;

    public static string PasswordReset(string fullName, string resetLink) => Layout(
        title: "Restablecer tu contraseña",
        body: $"""
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>Hola <strong>{Escape(fullName)}</strong>,</p>
            <p style='font-size:16px;line-height:1.6;margin:0 0 16px 0;'>
                Recibimos una solicitud para restablecer la contraseña de tu cuenta.
                Si fuiste vos, hacé click en el botón; si no, ignorá este mensaje.
            </p>
            <div style='margin:32px 0;text-align:center;'>
                <a href='{Escape(resetLink)}'
                   style='display:inline-block;padding:12px 28px;
                          background:#7C3AED;color:#fff;text-decoration:none;
                          border-radius:8px;font-weight:600;'>
                    Restablecer contraseña
                </a>
            </div>
            <p style='font-size:14px;color:#6b7280;line-height:1.6;margin:0;'>
                El enlace expira en 1 hora por seguridad. Si el botón no funciona,
                copiá y pegá este URL en el navegador:<br />
                <span style='word-break:break-all;color:#7C3AED;'>{Escape(resetLink)}</span>
            </p>
            """);

    private static string Layout(string title, string body) => $"""
        <!DOCTYPE html>
        <html lang='es'>
        <head><meta charset='utf-8'><title>{Escape(title)}</title></head>
        <body style='margin:0;padding:0;background:#0a0a0f;font-family:Arial,Helvetica,sans-serif;color:#f3f4f6;'>
            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#0a0a0f;padding:32px 16px;'>
                <tr><td align='center'>
                    <table role='presentation' width='560' cellpadding='0' cellspacing='0'
                           style='background:#161622;border:1px solid rgba(167,139,250,.2);border-radius:16px;overflow:hidden;'>
                        <tr><td style='padding:32px 32px 0 32px;'>
                            <div style='font-size:24px;font-weight:800;{BrandGradient}'>Gestor Financiero</div>
                        </td></tr>
                        <tr><td style='padding:24px 32px 32px 32px;color:#e5e7eb;'>
                            <h1 style='margin:0 0 16px 0;font-size:22px;color:#f3f4f6;'>{Escape(title)}</h1>
                            {body}
                        </td></tr>
                        <tr><td style='padding:24px 32px;border-top:1px solid rgba(255,255,255,.06);font-size:12px;color:#6b7280;'>
                            Este email fue enviado por Gestor Financiero. Si no esperabas recibirlo,
                            podés ignorarlo con seguridad.
                        </td></tr>
                    </table>
                </td></tr>
            </table>
        </body>
        </html>
        """;

    private static string Escape(string value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
