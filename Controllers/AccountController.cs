using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using gestor_financiero.Models;
using gestor_financiero.ViewModels;

namespace gestor_financiero.Controllers
{
    public class AccountController : Controller
    {
        private readonly FinanzasContext db = new FinanzasContext();
        private static readonly Random _rng = new Random();

        // =============== LOGIN ===============

        [AllowAnonymous]
        public ActionResult Login(string returnUrl, string email)
        {
            ViewBag.ReturnUrl = returnUrl;
            var model = new LoginViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (usuario == null || !Crypto.VerifyHashedPassword(usuario.PasswordHash, model.Password))
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            // Bloquear cuentas no verificadas y dar al usuario una manera de
            // recibir un OTP nuevo por si perdio el anterior.
            if (!usuario.Activo)
            {
                await GenerarYEnviarOtp(usuario, PropositoOtp.Registro);
                TempData["Info"] = "Tu cuenta aun no fue verificada. Te enviamos un nuevo codigo a tu correo.";
                return RedirectToAction("VerifyOtp", new { email = usuario.Email, proposito = PropositoOtp.Registro });
            }

            FormsAuthentication.SetAuthCookie(usuario.IdUsuario.ToString(), model.RememberMe);
            Session["UserName"] = usuario.Nombre;
            Session["UserId"] = usuario.IdUsuario;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // =============== REGISTRO (con OTP) ===============

        [AllowAnonymous]
        public ActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existente != null)
            {
                // Si la cuenta ya esta activa, no se puede volver a registrar.
                if (existente.Activo)
                {
                    ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
                    return View(model);
                }
                // Si existe pero no esta verificada, refrescamos sus datos
                // (en caso que cambie el nombre/password) y le mandamos OTP nuevo.
                existente.Nombre = model.Nombre;
                existente.PasswordHash = Crypto.HashPassword(model.Password);
                await GenerarYEnviarOtp(existente, PropositoOtp.Registro);
                await db.SaveChangesAsync();

                TempData["Info"] = "Esta cuenta ya estaba pendiente de verificacion. Te enviamos un codigo nuevo.";
                return RedirectToAction("VerifyOtp", new { email = existente.Email, proposito = PropositoOtp.Registro });
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                PasswordHash = Crypto.HashPassword(model.Password),
                FechaRegistro = DateTime.Now,
                Activo = false // se activa solo despues de verificar el OTP
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            await GenerarYEnviarOtp(usuario, PropositoOtp.Registro);

            TempData["Success"] = "Te enviamos un codigo a tu correo. Ingresalo para activar tu cuenta.";
            return RedirectToAction("VerifyOtp", new { email = usuario.Email, proposito = PropositoOtp.Registro });
        }

        // =============== VERIFICAR OTP ===============

        [AllowAnonymous]
        public ActionResult VerifyOtp(string email, string proposito)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(proposito))
                return RedirectToAction("Login");

            return View(new VerifyOtpViewModel
            {
                Email = email,
                Proposito = proposito
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario no encontrado.");
                return View(model);
            }

            var token = await db.PasswordResetTokens
                .Where(t => t.IdUsuario == usuario.IdUsuario
                         && t.Proposito == model.Proposito
                         && t.Token == model.Codigo
                         && !t.Usado)
                .OrderByDescending(t => t.CreadoEn)
                .FirstOrDefaultAsync();

            if (token == null || token.Expiracion < DateTime.UtcNow)
            {
                ModelState.AddModelError("Codigo", "Codigo invalido o expirado. Pedi uno nuevo.");
                return View(model);
            }

            token.Usado = true;
            await db.SaveChangesAsync();

            if (model.Proposito == PropositoOtp.Registro)
            {
                usuario.Activo = true;
                await db.SaveChangesAsync();

                TempData["Success"] = "Cuenta verificada. Inicia sesion para continuar.";
                return RedirectToAction("Login", new { email = usuario.Email });
            }

            // Reset password: el OTP esta validado, mostramos la pantalla
            // para que ingrese la nueva contrasena. Pasamos email + codigo
            // para que el POST de ResetPassword pueda volver a confirmar
            // que la verificacion ocurrio.
            return RedirectToAction("ResetPassword", new { email = usuario.Email, codigo = model.Codigo });
        }

        // Endpoint separado para "reenviar" OTP desde la pantalla de verificacion
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResendOtp(string email, string proposito)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario != null)
            {
                await GenerarYEnviarOtp(usuario, proposito);
                TempData["Success"] = "Te enviamos un codigo nuevo.";
            }
            else
            {
                TempData["Error"] = "No encontramos esa cuenta.";
            }
            return RedirectToAction("VerifyOtp", new { email, proposito });
        }

        // =============== OLVIDE MI CONTRASENA ===============

        [AllowAnonymous]
        public ActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Si existe, le mandamos OTP. Si no, igual mostramos el mismo
            // mensaje generico para no revelar si el email existe.
            if (usuario != null && usuario.Activo)
            {
                await GenerarYEnviarOtp(usuario, PropositoOtp.ResetPassword);
            }

            TempData["Success"] = "Si el correo esta registrado, te enviamos un codigo para restablecer la contrasena.";
            // Mandamos igual a la pantalla de verificacion: si el email no
            // existe, el codigo nunca matcheara, pero el usuario no se entera
            // de si el correo estaba o no en la base.
            return RedirectToAction("VerifyOtp", new { email = model.Email, proposito = PropositoOtp.ResetPassword });
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string email, string codigo)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo))
                return RedirectToAction("ForgotPassword");
            return View(new ResetPasswordViewModel { Email = email, Codigo = codigo });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (usuario == null)
            {
                TempData["Error"] = "No encontramos esa cuenta.";
                return RedirectToAction("ForgotPassword");
            }

            // El codigo tiene que existir y estar marcado como usado
            // (lo marcamos en VerifyOtp). Eso prueba que paso por la
            // verificacion. Ademas chequeamos que no haya pasado mucho tiempo
            // entre la verificacion y este POST (10 minutos).
            var token = await db.PasswordResetTokens
                .Where(t => t.IdUsuario == usuario.IdUsuario
                         && t.Proposito == PropositoOtp.ResetPassword
                         && t.Token == model.Codigo
                         && t.Usado)
                .OrderByDescending(t => t.CreadoEn)
                .FirstOrDefaultAsync();

            if (token == null || token.CreadoEn < DateTime.UtcNow.AddMinutes(-20))
            {
                TempData["Error"] = "La verificacion expiro. Volve a pedir un codigo.";
                return RedirectToAction("ForgotPassword");
            }

            usuario.PasswordHash = Crypto.HashPassword(model.Password);
            await db.SaveChangesAsync();

            TempData["Success"] = "Contrasena actualizada. Inicia sesion con la nueva.";
            return RedirectToAction("Login", new { email = usuario.Email });
        }

        // =============== CAMBIAR CONTRASENA (desde adentro) ===============

        [Authorize]
        public ActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int userId = int.Parse(User.Identity.Name);
            var usuario = await db.Usuarios.FindAsync(userId);
            if (usuario == null) return HttpNotFound();

            if (!Crypto.VerifyHashedPassword(usuario.PasswordHash, model.PasswordActual))
            {
                ModelState.AddModelError("PasswordActual", "La contrasena actual es incorrecta.");
                return View(model);
            }

            usuario.PasswordHash = Crypto.HashPassword(model.PasswordNueva);
            await db.SaveChangesAsync();

            TempData["Success"] = "Contrasena cambiada correctamente.";
            return RedirectToAction("Index", "Usuario");
        }

        // =============== LOGOUT ===============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // =============== HELPERS PRIVADOS ===============

        /// <summary>
        /// Invalida cualquier OTP pendiente del mismo proposito, genera uno
        /// nuevo de 6 digitos, lo persiste y lo envia por correo.
        /// </summary>
        private async Task GenerarYEnviarOtp(Usuario usuario, string proposito)
        {
            // Invalidar OTPs anteriores no usados del mismo proposito
            var anteriores = await db.PasswordResetTokens
                .Where(t => t.IdUsuario == usuario.IdUsuario
                         && t.Proposito == proposito
                         && !t.Usado)
                .ToListAsync();
            foreach (var a in anteriores) a.Usado = true;

            // Codigo 6 digitos. Random.Next(0, 1000000) puede dar < 100000,
            // por eso PadLeft para no perder el cero a la izquierda.
            string codigo;
            lock (_rng) { codigo = _rng.Next(0, 1000000).ToString().PadLeft(6, '0'); }

            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                IdUsuario  = usuario.IdUsuario,
                Token      = codigo,
                Proposito  = proposito,
                Expiracion = DateTime.UtcNow.AddMinutes(10),
                Usado      = false,
                CreadoEn   = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            try
            {
                var email = new EmailService();
                string asunto = proposito == PropositoOtp.Registro
                    ? "Activa tu cuenta - codigo de verificacion"
                    : "Restablecer contrasena - codigo de verificacion";
                string html = proposito == PropositoOtp.Registro
                    ? EmailService.BuildOtpRegistroHtml(usuario.Nombre, codigo)
                    : EmailService.BuildOtpResetHtml(usuario.Nombre, codigo);
                await email.SendAsync(usuario.Email, asunto, html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error enviando OTP: " + ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
