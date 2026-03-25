using System;
using System.Collections.Generic;

namespace AdsumPater.Models
{
    // --- ENUMS ---
    public enum EstadoPublicacion { Pendiente = 0, Aprobada = 1, Rechazada = 2 }
    
    public enum RolEquipo 
    { 
        Rector, ComunicacionYpeña, Infraestructura, ViaCrucis, 
        Espiritualidad, Coro, Apostolado, Asesor, Asesora 
    }

    // --- CLASES ---

    public class Intencion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nombre { get; set; } = "";
        public string Texto { get; set; } = "";
        public DateTimeOffset Fecha { get; set; } = DateTimeOffset.Now;
        public int Rezos { get; set; } = 0;
        public string? AdminToken { get; set; }
    }

    public class Reflexion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Contenido { get; set; } = "";
    public string PdfUrl { get; set; } = "";     
    public string PortadaUrl { get; set; } = ""; 
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.Now;
    public string? AdminToken { get; set; }
}


    public class Testimonio
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Autor { get; set; } = "";
        public string Texto { get; set; } = "";
        public string Rol { get; set; } = "Misionero"; 
        public string? AdminToken { get; set; }
    }

    // --- PUEBLO (ACTUALIZADO) ---
    public class PuebloViewModel
    {
        public string Nombre { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Pais { get; set; } = "Argentina";
        public string UbicacionTexto { get; set; } = "";
        public string Parroquia { get; set; } = "";
        public string FechaMision { get; set; } = "";
        public string Historia { get; set; } = "";
        public string Comunidad { get; set; } = "";
        public string MapaEmbedUrl { get; set; } = "";
        public DateTimeOffset FechaRevelacion { get; set; }
        
        // Listas para la nueva funcionalidad
        public List<ActividadPueblo> Actividades { get; set; } = new();
        public List<DiaCrono> Cronograma { get; set; } = new();
        
        public List<string> Necesidades { get; set; } = new();
        public List<string> Objetivos { get; set; } = new();
        public List<Foto> Fotos { get; set; } = new();
        public string? AdminToken { get; set; }
    }

    // Clase para las tarjetas de actividades con iconos de la grilla
    public class ActividadPueblo 
    {
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Icono { get; set; } = "star"; // Nombre de Material Icon (ej: church, groups, etc)
    }

    public class DiaCrono 
    { 
        public string Dia { get; set; } = ""; // Ej: "Jueves Santo"
        public string Titulo { get; set; } = ""; // Ej: "Día de la caridad"
        public List<Slot> Actividades { get; set; } = new();
        
        public DiaCrono() {}
        public DiaCrono(string d, string t) { Dia = d; Titulo = t; }
    }

    public class Slot 
    { 
        public string Hora { get; set; } = ""; 
        public string Descripcion { get; set; } = ""; 
        
        public Slot() {}
        public Slot(string h, string d) { Hora = h; Descripcion = d; }
    }

    // --- GALERÍA ---
    public class AlbumGaleria
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Anio { get; set; } = DateTime.Now.Year;
        public string Pueblo { get; set; } = "";
        public List<Foto> Fotos { get; set; } = new();
        public string? AdminToken { get; set; }
    }

    public class Foto 
    { 
        public string Url { get; set; } = ""; 
        public string Alt { get; set; } = ""; 
        public string Caption { get; set; } = "";     
        public string Descripcion { get; set; } = ""; 
        
        public string? AdminToken { get; set; }
        public Foto() {}
        public Foto(string u, string a, string d) { 
            Url = u; 
            Alt = a; 
            Descripcion = d;
            Caption = d; 
        }
    }
}