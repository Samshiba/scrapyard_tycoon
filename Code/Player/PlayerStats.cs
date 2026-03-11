using Sandbox;

public sealed class PlayerStats : Component
{
    public static PlayerStats Local { get; private set; }
    [Property] public float TotalScrap { get; private set; } = 0;

    protected override void OnAwake()
    {
        if ( !IsProxy )
            Local = this;

        if ( !IsProxy && SaveManager.Instance?.Data?.Player != null )
        {
            TotalScrap = SaveManager.Instance.Data.Player.TotalScrap;
            Log.Info( $"💰 TotalScrap chargé : {TotalScrap}" );
        }
    }

    public void AddScrap( float amount )
    {
        if ( amount <= 0 ) return;

        TotalScrap += amount;
        Log.Info( $"Vente réussie ! +{amount} Scrap. Total en banque : {TotalScrap}" );
        SaveChanges();
    }

    public bool SpendScrap( float amount )
    {
        if ( TotalScrap >= amount )
        {
            TotalScrap -= amount;
            SaveChanges();
            return true;
        }
        return false;
    }

    private void SaveChanges()
    {
        if ( !IsProxy && SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Player.TotalScrap = TotalScrap;
            SaveManager.Instance.Save();
        }
    }
}