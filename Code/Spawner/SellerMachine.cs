using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class SellerMachine : Component, Component.ITriggerListener
{
    private PlayerStats _linkedBank;

    [Property, Group( "Stats Tycoon" )] public float ProcessRate { get; set; } = 0.5f;
    [Property, Group( "Stats Tycoon" )] public float ValueMultiplier { get; set; } = 1.0f;
    [Property, Group( "Stats Tycoon" )] public int MaxQueueSize { get; set; } = 10;

    public Queue<ItemData> ProcessingQueue { get; private set; } = new();
    private TimeSince _timeSinceLastProcess;

    public void OnTriggerEnter( Collider other )
    {
        // --- CAS 1 : Le Joueur rentre dans la zone pour vider son sac ---
        var backpack = other.GameObject.Components.GetInAncestorsOrSelf<PlayerBackpack>();
        if ( backpack != null )
        {
            int itemsTransferred = 0;

            // On transfère le contenu du sac vers la machine
            while ( backpack.CollectedItems.Count > 0 && ProcessingQueue.Count < MaxQueueSize )
            {
                // On prend le dernier objet du sac
                var item = backpack.CollectedItems[^1];
                backpack.CollectedItems.RemoveAt( backpack.CollectedItems.Count - 1 );

                // On le met dans la file d'attente de la machine
                ProcessingQueue.Enqueue( item );
                itemsTransferred++;
            }

            if ( itemsTransferred > 0 )
            {
                Log.Info( $"Sac vidé ! {itemsTransferred} objets ajoutés à la machine." );
            }
            if ( backpack.CollectedItems.Count > 0 )
            {
                Log.Warning( "La machine est pleine ! Le reste reste dans votre sac." );
            }
            return; // On a géré le joueur, on s'arrête là.
        }

        // --- CAS 2 : Un déchet physique tombe dans la zone (pour plus tard avec les tapis roulants) ---
        var worldItem = other.GameObject.Components.GetInAncestorsOrSelf<IWorldItem>();
        if ( worldItem != null )
        {
            if ( ProcessingQueue.Count < MaxQueueSize )
            {
                ProcessingQueue.Enqueue( worldItem.GetItemData() );
                worldItem.Consume(); // Le déchet se détruit
            }
        }
    }

    public void OnTriggerExit( Collider other ) { }

    protected override void OnUpdate()
    {
        // RECHERCHE DYNAMIQUE DU JOUEUR (S'il n'est pas encore trouvé)
        if ( _linkedBank == null )
        {
            // On fouille la scène pour trouver le composant PlayerStats (parfait pour le runtime)
            _linkedBank = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

            // Si le joueur n'est pas encore spawn, on attend la prochaine frame
            if ( _linkedBank == null ) return;
        }

        // La logique Tycoon
        if ( ProcessingQueue.Count == 0 ) return;

        if ( _timeSinceLastProcess >= (1f / ProcessRate) )
        {
            _timeSinceLastProcess = 0;

            ItemData item = ProcessingQueue.Dequeue();
            float finalValue = item.Value * ValueMultiplier;

            _linkedBank.AddScrap( finalValue );
            Log.Info( $"⚙️ Traitement terminé : +{finalValue} Scrap." );
        }
        if ( ProcessingQueue.Count != 0 )
        {
            SaveChanges();
        }
    }

    private void SaveChanges()
    {
        if ( !IsProxy && SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Factory.SellerQueue = ProcessingQueue;
            SaveManager.Instance.Save();
        }
    }

    protected override void OnAwake()
    {
        if ( !IsProxy && SaveManager.Instance?.Data?.Factory != null )
        {
            ProcessingQueue = new Queue<ItemData>( SaveManager.Instance.Data.Factory.SellerQueue );
            Log.Info( $"SellerMachine : {ProcessingQueue.Count} items chargés dans la file d'attente." );
        }
    }
}