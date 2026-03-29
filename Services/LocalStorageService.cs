using Microsoft.JSInterop;
using System.Text.Json;

namespace AdsumPater.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            // Serializamos a JSON para mantener consistencia
            var json = JsonSerializer.Serialize(value);
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);

                if (string.IsNullOrWhiteSpace(json))
                    return default;

                try
                {
                    return JsonSerializer.Deserialize<T>(json);
                }
                catch (JsonException)
                {
                    // Si falla la deserialización pero el tipo pedido es string, 
                    // devolvemos el valor crudo. Esto evita el error que tenías con el token.
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)json;
                    }
                    return default;
                }
            }
            catch
            {
                return default;
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}