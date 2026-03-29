using System.Net.Http.Json;

namespace AdsumPater.Services
{
    public class FirebaseService
    {
        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private readonly string _baseUrl;

        private static readonly Dictionary<string, (DateTime Expira, object Valor)> _cache = new();

        public FirebaseService(HttpClient http, IConfiguration config, LocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;

        // Priorizamos appsettings.json, si no existe, lanzamos error o usamos un fallback
        var urlConfig = config["FirebaseUrl"];
        
        if (string.IsNullOrWhiteSpace(urlConfig))
        {
            // Opcional: Lanzar una excepción para que te des cuenta rápido si falta la config
            throw new Exception("Falta la configuración 'FirebaseUrl' en appsettings.json");
        }

        _baseUrl = urlConfig.EndsWith("/") ? urlConfig : urlConfig + "/";
    }

        private class CacheLocalItem<T>
        {
            public DateTime Expira { get; set; }
            public T? Valor { get; set; }
        }

        // --- MODIFICADO: Ahora es async y maneja el token de auth ---
        private async Task<string> ArmarUrl(string ruta, bool requiereAuth = false)
{
    ruta = ruta.Trim('/');
    var url = $"{_baseUrl}{ruta}.json";

    if (requiereAuth)
    {
        try 
        {
            // Intentamos obtenerlo. Si tu LocalStorageService usa GetItemAsync<T>, 
            // a veces falla si el valor guardado por JS no tiene comillas de JSON.
            var token = await _localStorage.GetItemAsync<string>("firebaseToken");
            
            if (!string.IsNullOrEmpty(token))
            {
                url += $"?auth={token}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error obteniendo el token: " + ex.Message);
            // Si falla, la URL va sin auth y Firebase dará 401, pero la app no crashea
        }
    }
    return url;
}

        #region Caché Helpers
        private bool TryGetCache<T>(string key, out T? valor)
        {
            valor = default;
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.Expira > DateTime.UtcNow && item.Valor is T casteado)
                {
                    valor = casteado;
                    return true;
                }
                _cache.Remove(key);
            }
            return false;
        }

        private void SetCache<T>(string key, T valor, int cacheSegundos)
        {
            _cache[key] = (DateTime.UtcNow.AddSeconds(cacheSegundos), valor!);
        }

        private async Task<T?> GetLocalCacheAsync<T>(string key)
        {
            try
            {
                var item = await _localStorage.GetItemAsync<CacheLocalItem<T>>(key);
                if (item == null) return default;
                if (item.Expira <= DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync(key);
                    return default;
                }
                return item.Valor;
            }
            catch { return default; }
        }

        private async Task SetLocalCacheAsync<T>(string key, T valor, int cacheSegundos)
        {
            try
            {
                var item = new CacheLocalItem<T> { Expira = DateTime.UtcNow.AddSeconds(cacheSegundos), Valor = valor };
                await _localStorage.SetItemAsync(key, item);
            }
            catch { }
        }

        private async Task RemoveLocalCacheAsync(string key)
        {
            try { await _localStorage.RemoveItemAsync(key); } catch { }
        }

        public async Task LimpiarCache(string? empiezaCon = null)
        {
            if (string.IsNullOrWhiteSpace(empiezaCon))
            {
                _cache.Clear();
                return;
            }
            var claves = _cache.Keys.Where(k => k.Contains($":{empiezaCon}")).ToList();
            foreach (var clave in claves) _cache.Remove(clave);

            await RemoveLocalCacheAsync($"lista:{empiezaCon}");
            await RemoveLocalCacheAsync($"dic:{empiezaCon}");
            await RemoveLocalCacheAsync($"obj:{empiezaCon}");
        }
        #endregion

        #region Métodos de Escritura (REQUIEREN AUTH)

        public async Task<bool> Agregar<T>(string coleccion, T entidad)
        {
            var url = await ArmarUrl(coleccion, requiereAuth: true);
            var response = await _http.PostAsJsonAsync(url, entidad);
            if (response.IsSuccessStatusCode) await LimpiarCache(coleccion);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> GuardarObjeto<T>(string nodo, T objeto)
        {
            var url = await ArmarUrl(nodo, requiereAuth: true);
            var response = await _http.PutAsJsonAsync(url, objeto);
            if (response.IsSuccessStatusCode) await LimpiarCache(nodo);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarObjeto<T>(string nodo, string key, T objeto)
        {
            var url = await ArmarUrl($"{nodo}/{key}", requiereAuth: true);
            var response = await _http.PutAsJsonAsync(url, objeto);
            if (response.IsSuccessStatusCode) await LimpiarCache(nodo);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarCampo(string coleccion, string idFirebase, string campo, object valor)
        {
            var url = await ArmarUrl($"{coleccion}/{idFirebase}/{campo}", requiereAuth: true);
            var response = await _http.PutAsJsonAsync(url, valor);
            if (response.IsSuccessStatusCode) await LimpiarCache(coleccion);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Borrar(string nodo, string id)
        {
            var url = await ArmarUrl($"{nodo}/{id}", requiereAuth: true);
            var response = await _http.DeleteAsync(url);
            if (response.IsSuccessStatusCode) await LimpiarCache(nodo);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Métodos de Lectura (PÚBLICOS)

        public async Task<List<T>> ObtenerLista<T>(string coleccion, int cacheSegundos = 3600)
        {
            var cacheKey = $"lista:{coleccion}";
            if (cacheSegundos > 0 && TryGetCache(cacheKey, out List<T>? listaMemoria)) return listaMemoria!;

            var url = await ArmarUrl(coleccion); // requiereAuth es false por defecto
            var response = await _http.GetFromJsonAsync<Dictionary<string, T>>(url);
            var lista = response == null ? new List<T>() : response.Values.ToList();

            if (cacheSegundos > 0)
            {
                SetCache(cacheKey, lista, cacheSegundos);
                await SetLocalCacheAsync(cacheKey, lista, cacheSegundos);
            }
            return lista;
        }

        public async Task<Dictionary<string, T>> ObtenerDiccionario<T>(string coleccion, int cacheSegundos = 3600)
        {
            var cacheKey = $"dic:{coleccion}";
            if (cacheSegundos > 0 && TryGetCache(cacheKey, out Dictionary<string, T>? dicMemoria)) return dicMemoria!;

            var url = await ArmarUrl(coleccion);
            var dic = await _http.GetFromJsonAsync<Dictionary<string, T>>(url) ?? new Dictionary<string, T>();

            if (cacheSegundos > 0)
            {
                SetCache(cacheKey, dic, cacheSegundos);
                await SetLocalCacheAsync(cacheKey, dic, cacheSegundos);
            }
            return dic;
        }

        public async Task<T?> ObtenerObjeto<T>(string nodo, int cacheSegundos = 3600)
        {
            var cacheKey = $"obj:{nodo}";
            if (cacheSegundos > 0 && TryGetCache(cacheKey, out T? objMemoria)) return objMemoria;

            var url = await ArmarUrl(nodo);
            var obj = await _http.GetFromJsonAsync<T>(url);

            if (obj is not null && cacheSegundos > 0)
            {
                SetCache(cacheKey, obj, cacheSegundos);
                await SetLocalCacheAsync(cacheKey, obj, cacheSegundos);
            }
            return obj;
        }

        #endregion

        public bool EsUrlArchivoValida(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out _) || url.StartsWith("/");
        }
    }
}