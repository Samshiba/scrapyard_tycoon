using Sandbox;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;

/// <summary>
/// Conteneur pour les traductions d'un domaine.
/// Explorable dans l'inspecteur.
/// </summary>
[AssetType( Name = "translation_domain" )]
public class TranslationDomain
{
    [Property] public Dictionary<string, string> Translations { get; set; } = new();
}

/// <summary>
/// Gestionnaire centralisé de localisation (i18n).
/// Charge les fichiers JSON de traduction par domaine et par langue.
/// Singleton accessible globallement via LocalizationManager.GetText(key)
/// </summary>
public sealed class LocalizationManager : Component
{
    public static LocalizationManager Instance { get; private set; }

    private string _currentLanguage = "fr";
    [Property] private Dictionary<string, TranslationDomain> _translations = new();

    protected override void OnAwake()
    {
        Instance = this;
    }

    private static readonly string[] _translationDomains =
    {
        "common",
        "ui",
        "weapon",
        "upgrade",
        "tooltip"
    };

    private static readonly string _localizationPath = "Localization";

    public LocalizationManager()
    {
        Instance = this;
    }

    /// <summary>
    /// Initialiser le gestionnaire de localisation avec la langue spécifiée.
    /// Appeler une seule fois au démarrage du jeu.
    /// </summary>
    public static void Initialize( string language = "fr" )
    {
        if ( Instance == null )
        {
            Instance = new LocalizationManager();
        }

        Instance.LoadLanguage( language );
    }

    /// <summary>
    /// Charger tous les fichiers JSON de la langue spécifiée.
    /// </summary>
    private void LoadLanguage( string language )
    {
        _currentLanguage = language.ToLower();
        _translations.Clear();

        Log.Info( "domains: " + string.Join( ", ", _translationDomains ) );

        foreach ( var domain in _translationDomains )
        {
            var filePath = Path.Combine( _localizationPath, _currentLanguage, $"{domain}.json" );
            LoadDomainFile( domain, filePath );
        }

        Log.Info( $"[LocalizationManager] Language '{_currentLanguage}' loaded with {_translations.Count} domain(s)" );
    }

    /// <summary>
    /// Charger un fichier JSON d'un domaine particulier.
    /// </summary>
    private void LoadDomainFile( string domain, string filePath )
    {
        try
        {
            if ( !FileSystem.Mounted.FileExists( filePath ) )
            {
                Log.Warning( $"[LocalizationManager] WARNING: Missing translation file '{filePath}'" );
                _translations[domain] = new TranslationDomain { Translations = new Dictionary<string, string>() };
                return;
            }

            var fileContent = FileSystem.Mounted.ReadAllText( filePath );
            var jsonNode = JsonNode.Parse( fileContent );
            var domainDict = new Dictionary<string, string>();

            if ( jsonNode is JsonObject jsonObj )
            {
                foreach ( var kvp in jsonObj )
                {
                    domainDict[kvp.Key] = kvp.Value?.GetValue<string>() ?? "[NULL]";
                }
            }
            Log.Info( domainDict );

            _translations[domain] = new TranslationDomain { Translations = domainDict };
            Log.Info( $"[LocalizationManager] Loaded domain '{domain}.json': {domainDict.Count} key(s)" );
        }
        catch ( System.Exception e )
        {
            Log.Error( $"[LocalizationManager] ERROR: Failed to load translation file '{filePath}' - {e.Message}" );
            _translations[domain] = new TranslationDomain { Translations = new Dictionary<string, string>() };
        }
    }

    /// <summary>
    /// Récupérer un texte traduit par clé. Format: "domain.key" (ex: "ui.shop.title")
    /// Supporte aussi les placeholders {0}, {1}, etc. via GetText(key, param1, param2, ...)
    /// </summary>
    public static string GetText( string key, params object[] parameters )
    {
        if ( Instance == null )
        {
            Log.Warning( "[LocalizationManager] WARNING: Not initialized. Call Initialize() at startup." );
            return $"[MISSING: {key}]";
        }

        var text = Instance.GetTextInternal( key, null );

        // Null-safety check
        if ( text == null )
        {
            Log.Warning( $"[LocalizationManager] WARNING: GetTextInternal returned null for key '{key}'" );
            return $"[NULL_RESULT: {key}]";
        }

        // Si paramètres fournis, utiliser String.Format
        if ( parameters != null && parameters.Length > 0 )
        {
            try
            {
                return string.Format( text, parameters );
            }
            catch ( System.Exception e )
            {
                Log.Warning( $"[LocalizationManager] WARNING: Failed to format localization key '{key}' - {e.Message}" );
                return text;
            }
        }

        return text;
    }

    private string GetTextInternal( string key, string fallback = null )
    {
        // Clé format: "domain.key" ou "domain.sub.key"
        var parts = key.Split( '.' );

        if ( parts.Length < 2 )
        {
            Log.Warning( $"[LocalizationManager] WARNING: Invalid localization key format '{key}'. Expected format: 'domain.key'" );
            return fallback ?? $"[INVALID_KEY: {key}]";
        }

        var domain = parts[0];

        if ( !_translations.ContainsKey( domain ) )
        {
            Log.Warning( $"[LocalizationManager] WARNING: Unknown localization domain '{domain}'" );
            return fallback ?? $"[MISSING_DOMAIN: {key}]";
        }

        var textKey = string.Join( ".", parts, 1, parts.Length - 1 );
        var domainDict = _translations[domain].Translations;

        if ( domainDict.ContainsKey( textKey ) )
        {
            return domainDict[textKey];
        }

        Log.Warning( $"[LocalizationManager] WARNING: Missing translation key '{textKey}' in domain '{domain}'" );
        return fallback ?? $"[MISSING: {textKey}]";
    }

    /// <summary>
    /// Changer la langue et recharger tous les fichiers JSON.
    /// </summary>
    public static void SetLanguage( string language )
    {
        if ( Instance == null )
        {
            Log.Error( "[LocalizationManager] ERROR: Not initialized." );
            return;
        }

        Instance.LoadLanguage( language );
        Log.Info( $"[LocalizationManager] Language changed to '{language}'" );
    }

    /// <summary>
    /// Récupérer la langue actuelle.
    /// </summary>
    public static string GetLanguage() => Instance?._currentLanguage ?? "fr";

    /// <summary>
    /// Récupérer tous les textes du domaine (debug/inspection).
    /// </summary>
    public static Dictionary<string, string> GetDomain( string domain )
    {
        if ( Instance == null || !Instance._translations.ContainsKey( domain ) )
        {
            return new Dictionary<string, string>();
        }

        return Instance._translations[domain].Translations;
    }
}
