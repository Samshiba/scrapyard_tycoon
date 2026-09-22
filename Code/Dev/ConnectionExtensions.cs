using Sandbox;

public static class ConnectionExtensions
{
    /// <summary>
    /// Génère un identifiant unique sûr. Si c'est le 2ème client local de test,
    /// on génère un faux SteamID pour éviter les collisions.
    /// </summary>
    public static string GetUniqueId( this Connection channel )
    {
        if ( !channel.IsHost && channel.SteamId == Connection.Local.SteamId )
        {
            return $"DEV_CLONE_{channel.Id}";
        }

        return channel.SteamId.ToString();
    }
}