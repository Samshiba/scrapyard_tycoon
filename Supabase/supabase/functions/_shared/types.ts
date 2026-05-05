export interface ItemStack {
    type: number;
    value: number;
    count: number;
}

export interface WorldStateData {
    tier: number;
    run_scrap_gained: number;
    global_upgrades: Record<string, number>;
    unlocked_prestige_upgrades: Record<string, boolean>;
    unlocked_weapons: string[];
    unlocked_utilities: string[];
    machine_upgrades: Record<string, number>;
    seller_queue: ItemStack[];
}

export interface FactoryWorldData {
    id: string;
    host_steam_id: string;
    save_name: string;
    version: string;
    is_sandbox: boolean;
    current_scrap: number;
    prestige_points: number;
    world_state: WorldStateData;
    statistics: any; // TODO: Define the structure
}

export interface InventoryStateData {
    equipped_weapons: (string | null)[];
    active_weapon_index: number;
    collected_items: ItemStack[];
}

export interface PlayerSessionData {
    steam_id: string;
    factory_id: string;
    vip_status: boolean;
    cosmetic_state: any; // TODO: Define the structure
    inventory_state: InventoryStateData;
    statistics: any; // TODO: Define the structure
}

export interface SyncPayload {
    factory_id: string;
    factory_data?: Partial<FactoryWorldData>;
    players_data?: Partial<PlayerSessionData>[];
}
