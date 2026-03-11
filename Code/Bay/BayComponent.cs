using Sandbox;
public sealed class BayComponent : Component
{
    [Property] public int BayId { get; set; }
    public Connection Owner { get; private set; }
    public bool IsOccupied => Owner != null;

    [Property] public GameObject SpawnPoint { get; set; }
    [Property] public GameObject HopperArea { get; set; }
    [Property] public GameObject PlayerStart { get; set; }

    public void AssignOwner( Connection channel )
    {
        Owner = channel;
        Log.Info( $"Baie {BayId} assignée à {channel.DisplayName}" );
        
        // TODO: Apply visual upgrades from SaveData.Prestige.UnlockedPrestigeUpgrades
    }

    public void ClearOwner()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }
        Owner = null;
    }
}