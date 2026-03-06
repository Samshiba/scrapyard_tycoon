using Sandbox;

public sealed class PlayerStats : Component
{
    [Sync][Property] public float TotalScrap { get; private set; } = 0;

    public void AddScrap(float amount)
    {
        if (amount <= 0) return;

        TotalScrap += amount;
        Log.Info($"Vente réussie ! +{amount} Scrap. Total en banque : {TotalScrap}");
    }

    public bool SpendScrap(float amount)
    {
        if (TotalScrap >= amount)
        {
            TotalScrap -= amount;
            return true;
        }
        return false;
    }
}