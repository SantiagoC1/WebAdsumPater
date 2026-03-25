using System.Net.Http.Json;

namespace AdsumPater.Services
{
    public class FirebaseService
    {
        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private string _baseUrl = "https://adsum-pater-web-default-rtdb.firebaseio.com/";

        private static readonly Dictionary<string, (DateTime Expira, object Valor)> _cache = new();

        public FirebaseService(HttpClient http, IConfiguration config, LocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;

            var urlConfig = config["FirebaseUrl"];
            if (!string.IsNullOrWhiteSpace(urlConfig))
                _baseUrl = urlConfig;

            if (!_baseUrl.EndsWith("/"))
                _baseUrl += "/";
        }

        private class CacheLocalItem<T>
        {
            public DateTime Expira { get; set; }
            public T? Valor { get; set; }
        }

        private string ArmarUrl(string ruta)
        {
            ruta = ruta.Trim('/');
            return $"{_baseUrl}{ruta}.json";
        }

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

                if (item == null)
                    return default;

                if (item.Expira <= DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync(key);
                    return default;
                }

                return item.Valor;
            }
            catch
            {
                return default;
            }
        }

        private async Task SetLocalCacheAsync<T>(string key, T valor, int cacheSegundos)
        {
            try
            {
                var item = new CacheLocalItem<T>
                {
                    Expira = DateTime.UtcNow.AddSeconds(cacheSegundos),
                    Valor = valor
                };

                await _localStorage.SetItemAsync(key, item);
            }
            catch
            {
                // Si falla localStorage, no rompemos la app.
            }
        }

        private async Task RemoveLocalCacheAsync(string key)
        {
            try
            {
                await _localStorage.RemoveItemAsync(key);
            }
            catch
            {
                // No hacemos nada si falla.
            }
        }

        public async Task LimpiarCache(string? empiezaCon = null)
        {
            if (string.IsNullOrWhiteSpace(empiezaCon))
            {
                _cache.Clear();
                return;
            }

            var claves = _cache.Keys
                .Where(k =>
                    k.StartsWith($"lista:{empiezaCon}", StringComparison.OrdinalIgnoreCase) ||
                    k.StartsWith($"dic:{empiezaCon}", StringComparison.OrdinalIgnoreCase) ||
                    k.StartsWith($"obj:{empiezaCon}", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var clave in claves)
                _cache.Remove(clave);

            await RemoveLocalCacheAsync($"lista:{empiezaCon}");
            await RemoveLocalCacheAsync($"dic:{empiezaCon}");
            await RemoveLocalCacheAsync($"obj:{empiezaCon}");
        }

        public async Task<bool> Agregar<T>(string coleccion, T entidad)
        {
            var url = ArmarUrl(coleccion);
            var response = await _http.PostAsJsonAsync(url, entidad);

            if (response.IsSuccessStatusCode)
                await LimpiarCache(coleccion);

            return response.IsSuccessStatusCode;
        }

        public async Task<List<T>> ObtenerLista<T>(string coleccion, int cacheSegundos = 3600)
{
    var cacheKey = $"lista:{coleccion}";

    if (cacheSegundos > 0 && TryGetCache(cacheKey, out List<T>? listaMemoria))
        return listaMemoria!;

    if (cacheSegundos > 0)
    {
        var listaLocal = await GetLocalCacheAsync<List<T>>(cacheKey);
        if (listaLocal != null)
        {
            SetCache(cacheKey, listaLocal, cacheSegundos);
            return listaLocal;
        }
    }

    var url = ArmarUrl(coleccion);
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

    if (cacheSegundos > 0 && TryGetCache(cacheKey, out Dictionary<string, T>? dicMemoria))
        return dicMemoria!;

    if (cacheSegundos > 0)
    {
        var dicLocal = await GetLocalCacheAsync<Dictionary<string, T>>(cacheKey);
        if (dicLocal != null)
        {
            SetCache(cacheKey, dicLocal, cacheSegundos);
            return dicLocal;
        }
    }

    var url = ArmarUrl(coleccion);
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

    if (cacheSegundos > 0 && TryGetCache(cacheKey, out T? objMemoria))
        return objMemoria;

    if (cacheSegundos > 0)
    {
        var objLocal = await GetLocalCacheAsync<T>(cacheKey);
        if (objLocal != null)
        {
            SetCache(cacheKey, objLocal, cacheSegundos);
            return objLocal;
        }
    }

    var url = ArmarUrl(nodo);
    var obj = await _http.GetFromJsonAsync<T>(url);

    if (obj is not null && cacheSegundos > 0)
    {
        SetCache(cacheKey, obj, cacheSegundos);
        await SetLocalCacheAsync(cacheKey, obj, cacheSegundos);
    }

    return obj;
}

        public async Task<bool> GuardarObjeto<T>(string nodo, T objeto)
        {
            var url = ArmarUrl(nodo);
            var response = await _http.PutAsJsonAsync(url, objeto);

            if (response.IsSuccessStatusCode)
                await LimpiarCache(nodo);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarCampo(string coleccion, string idFirebase, string campo, object valor)
        {
            var url = ArmarUrl($"{coleccion}/{idFirebase}/{campo}");
            var response = await _http.PutAsJsonAsync(url, valor);

            if (response.IsSuccessStatusCode)
                await LimpiarCache(coleccion);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarObjeto<T>(string nodo, string key, T objeto)
        {
            var url = ArmarUrl($"{nodo}/{key}");
            var response = await _http.PutAsJsonAsync(url, objeto);

            if (response.IsSuccessStatusCode)
                await LimpiarCache(nodo);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Borrar(string nodo, string id)
        {
            var url = ArmarUrl($"{nodo}/{id}");
            var response = await _http.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
                await LimpiarCache(nodo);

            return response.IsSuccessStatusCode;
        }

        public bool EsUrlArchivoValida(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out _) ||
                   url.StartsWith("/", StringComparison.OrdinalIgnoreCase);
        }
    }
}