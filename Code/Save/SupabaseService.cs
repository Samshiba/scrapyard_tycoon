using Sandbox;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/// <summary>
/// Cloud-Based Sync Service.
/// Implements unified endpoint-based synchronization with Steam authentication and anti-cheat validation.
/// </summary>
public static class SupabaseService
{
    private const string SUPABASE_URL = "https://vesvpjskpjurjjavznvt.supabase.co";
    private const string SYNC_ENDPOINT = "/functions/v1/sync";
    private const string SUPABASE_ANON_KEY = "sb_publishable_SmqylJW2aJ7lSZYfY50TNA_eF9JIf7B";

    // ==========================================
    // DATA MODELS
    // ==========================================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private class SyncPayload
    {
        [JsonPropertyName( "factory_id" )]
        public string FactoryId { get; set; }

        [JsonPropertyName( "factory_data" )]
        public FactoryWorldData FactoryData { get; set; }

        [JsonPropertyName( "players_data" )]
        public List<PlayerSessionData> PlayersData { get; set; }
    }

    private class SyncResponse
    {
        [JsonPropertyName( "status" )]
        public string Status { get; set; }

        [JsonPropertyName( "type" )]
        public string Type { get; set; }

        [JsonPropertyName( "message" )]
        public string Message { get; set; }

        [JsonPropertyName( "is_sandbox" )]
        public bool IsSandbox { get; set; }

        [JsonPropertyName( "error" )]
        public string Error { get; set; }
    }

    public class FullSyncResponse
    {
        [JsonPropertyName( "status" )] public string Status { get; set; }
        [JsonPropertyName( "factory_data" )] public FactoryWorldData FactoryData { get; set; }
        [JsonPropertyName( "players_data" )] public List<PlayerSessionData> PlayersData { get; set; }
    }

    public class FactoryListResponse
    {
        [JsonPropertyName( "status" )] public string Status { get; set; }
        [JsonPropertyName( "factories" )] public List<FactoryWorldData> Factories { get; set; }
    }

    private static Dictionary<string, string> GetSyncHeaders( string sboxToken, string steamId )
    {
        return new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "X-sbox-Auth-Token", sboxToken },
            { "X-Steam-Id", steamId },
            { "apikey", SUPABASE_ANON_KEY },
            { "X-Admin-Key", "WWSMTHjX8ji4WtT@cvPZai2fjb#@1c" }
        };
    }

    // ==========================================
    // SYNC GET
    // ==========================================

    public static async Task<(FactoryWorldData Factory, List<PlayerSessionData> Players)> FetchWorldStateAsync( string factoryId, string sboxToken, string steamId )
    {
        try
        {
            var headers = GetSyncHeaders( sboxToken, steamId );
            var url = $"{SUPABASE_URL}{SYNC_ENDPOINT}?factory_id={factoryId}";

            var response = await Http.RequestAsync( url, "GET", null, headers );

            if ( !response.IsSuccessStatusCode )
            {
                Log.Warning( $"[Supabase] FetchWorldState failed: {response.StatusCode}, Body: {await response.Content.ReadAsStringAsync()}" );
                return (null, null);
            }

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FullSyncResponse>( body, JsonOptions );

            if ( data?.Status == "success" )
            {
                return (data.FactoryData, data.PlayersData);
            }
        }
        catch ( System.Exception ex )
        {
            Log.Error( $"[Supabase] Fetch error: {ex.Message}" );
        }

        return (null, null);
    }

    // ==========================================
    // SYNC POST
    // ==========================================

    public static async Task<bool> SyncAsync(
        string factoryId,
        FactoryWorldData factoryData,
        List<PlayerSessionData> playersData,
        string sboxToken,
        string steamId )
    {
        try
        {
            var payload = new SyncPayload
            {
                FactoryId = factoryId,
                FactoryData = factoryData,
                PlayersData = playersData
            };

            var headers = GetSyncHeaders( sboxToken, steamId );
            var jsonString = JsonSerializer.Serialize( payload, JsonOptions );
            var content = new StringContent( jsonString, Encoding.UTF8, "application/json" );

            var fullUrl = $"{SUPABASE_URL}{SYNC_ENDPOINT}";
            var response = await Http.RequestAsync( fullUrl, "POST", content, headers );

            if ( !response.IsSuccessStatusCode )
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Log.Error( $"[Supabase] Sync failed. Status: {response.StatusCode}. Body: {errorBody}" );
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var syncResponse = JsonSerializer.Deserialize<SyncResponse>( responseBody, JsonOptions );

            if ( syncResponse?.Status != "success" )
            {
                Log.Warning( $"[Supabase] Sync completed with warnings - Type: {syncResponse?.Type}, Message: {syncResponse?.Message}" );
            }
            else
            {
                if ( SaveConfig.DEBUG_SAVE_LOGGING )
                    Log.Info( $"[Supabase] Sync successful - {syncResponse.Message}" );
            }

            // Check if factory was flagged as sandbox due to cheat detection
            if ( syncResponse?.IsSandbox == true && !factoryData.IsSandbox )
            {
                Log.Warning( "[Supabase] Factory flagged as Sandbox mode due to anti-cheat detection!" );
            }

            return true;
        }
        catch ( System.Exception ex )
        {
            Log.Error( $"[Supabase] Sync error: {ex.Message}" );
            return false;
        }
    }

    // ==========================================
    // FACTORY_LIST GET
    // ==========================================
    public static async Task<List<FactoryWorldData>> FetchUserFactoriesAsync( string steamId, string sboxToken )
    {
        try
        {
            var headers = GetSyncHeaders( sboxToken, steamId );
            var url = $"{SUPABASE_URL}/functions/v1/list_factories";

            var response = await Http.RequestAsync( url, "GET", null, headers );

            if ( !response.IsSuccessStatusCode )
            {
                Log.Warning( $"[Supabase] FetchWorldState failed: {response.StatusCode}, Body: {await response.Content.ReadAsStringAsync()}" );
                return new List<FactoryWorldData>();
            }

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FactoryListResponse>( body, JsonOptions );

            if ( data?.Status == "success" && data.Factories != null )
            {
                return data.Factories;
            }
        }
        catch ( System.Exception ex )
        {
            Log.Error( $"[Supabase] FetchUserFactories error: {ex.Message}" );
        }

        return new List<FactoryWorldData>();
    }


    public static async Task<bool> SyncFactoryAsync( string factoryId, FactoryWorldData factoryData, string sboxToken, string steamId )
    {
        return await SyncAsync( factoryId, factoryData, null, sboxToken, steamId );
    }

    public static async Task<bool> SyncPlayerAsync( string factoryId, PlayerSessionData playerData, string sboxToken, string steamId )
    {
        var playersList = new List<PlayerSessionData> { playerData };
        return await SyncAsync( factoryId, null, playersList, sboxToken, steamId );
    }
}