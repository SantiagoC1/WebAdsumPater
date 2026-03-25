namespace AdsumPater.Models
{
    public class Edicion
    {
        public int Anio { get; set; }
        public string Lugar { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public string Parroquia { get; set; } = "";
        public string Enfoque { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string? Nota { get; set; }
        public string? AdminToken { get; set; }

        // URLs de imágenes
        public List<string> Fotos { get; set; } = new();
        public List<Testimonio> Testimonios { get; set; } = new();
    }
}
