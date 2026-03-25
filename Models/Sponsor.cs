namespace AdsumPater.Models
{
    public class Sponsor
    {
        public string Nombre { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty; // URL de la imagen en Firebase Storage
        public string LinkWeb { get; set; } = string.Empty; // Para redirigir al hacer clic
        public string Categoria { get; set; } = "Colaborador"; // Ej: "Main", "Colaborador", "Donante"
        public bool Activo { get; set; } = true;
        public string? AdminToken { get; set; }
    }
}