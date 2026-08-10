using System.ComponentModel.DataAnnotations;

namespace gestor_financiero.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo")]
        [EmailAddress(ErrorMessage = "Formato de correo no válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese su contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }

    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Ingresá tu correo")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }
    }

    /// <summary>
    /// VM compartido para validar un OTP, tanto en el flujo de registro
    /// como en el de reset de contrasena. El controller usa "Proposito"
    /// para decidir si despues redirige a Login o a la pantalla de
    /// nueva contrasena.
    /// </summary>
    public class VerifyOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingresá el código que te enviamos")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código tiene 6 dígitos")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Solo dígitos")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        // "REGISTRO" o "RESET_PASSWORD"
        [Required]
        public string Proposito { get; set; }
    }

    /// <summary>
    /// VM para la pantalla "Nueva contrasena" tras validar el OTP de reset.
    /// El email y un flag interno indican que ya pasamos la verificacion.
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Marca que el OTP previo fue validado (se setea desde el controller)
        [Required]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirmá la contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// VM para cambiar la contrasena desde el perfil (usuario ya autenticado).
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "Ingresá la nueva contraseña")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string PasswordNueva { get; set; }

        [Required(ErrorMessage = "Confirmá la nueva contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }
}
