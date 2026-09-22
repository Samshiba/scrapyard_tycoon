// Setup type definitions for built-in Supabase Runtime APIs
import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient, SupabaseClient } from "jsr:@supabase/supabase-js@2";
import { verifySboxToken } from "../_shared/auth.ts";
import {
  AuthenticationError,
  CheatingAttemptError,
  RepositoryError,
  ValidationError,
} from "../_shared/errors.ts";
import { corsHeaders, createJsonResponse } from "../_shared/cors.ts";
import {
  FactoryWorldData,
  PlayerSessionData,
  SyncPayload,
} from "../_shared/types.ts";

// --- 3. VALIDATION ---

class SyncValidator {
  private oldFactoryDataCache?: FactoryWorldData;
  private oldPlayersDataCache: Map<string, PlayerSessionData> = new Map();

  constructor(private db: SupabaseClient) {}

  async validateFactory(factoryId: string, factory: Partial<FactoryWorldData>) {
    if (!factory.id) throw new ValidationError("Factory ID missing", 400);
    if (factory.id !== factoryId) {
      throw new ValidationError("Factory factory_id mismatch", 400);
    }

    const oldFactory = await this.fetchSnapshot("factories", factory.id);

    // --- CHECK ANTI-SPAM ---
    if (oldFactory && oldFactory.last_updated) {
      const secondsSinceLastSave =
        (new Date().getTime() - new Date(oldFactory.last_updated).getTime()) /
        1000;
      if (secondsSinceLastSave < 1) { // 1 second threshold
        throw new CheatingAttemptError(
          "Rate limit exceeded: saves are too frequent.",
          429,
        );
      }
    }

    this.oldFactoryDataCache = oldFactory; // Cache for players checks and repository

    this.runFactoryChecks(oldFactory, factory);
  }

  async validatePlayer(factoryId: string, player: Partial<PlayerSessionData>) {
    if (!player.steam_id || !player.factory_id) {
      throw new ValidationError("Player steam_id or factory_id missing", 400);
    }
    if (player.factory_id !== factoryId) {
      throw new ValidationError("Player factory_id mismatch", 400);
    }

    const oldPlayer = await this.fetchSnapshot(
      "factory_players",
      player.steam_id,
      player.factory_id,
    );

    // --- CHECK ANTI-SPAM ---
    if (oldPlayer && oldPlayer.last_updated) {
      const secondsSinceLastSave =
        (new Date().getTime() - new Date(oldPlayer.last_updated).getTime()) /
        1000;
      if (secondsSinceLastSave < 1) { // 1 second threshold
        throw new CheatingAttemptError(
          "Rate limit exceeded: saves are too frequent.",
          429,
        );
      }
    }

    // Cache for repository
    const playerKey = `${player.steam_id}:${player.factory_id}`;
    if (oldPlayer) {
      this.oldPlayersDataCache.set(playerKey, oldPlayer);
    }

    if (!this.oldFactoryDataCache) {
      this.oldFactoryDataCache = await this.fetchSnapshot(
        "factories",
        player.factory_id,
      );
    }

    if (!this.oldFactoryDataCache) {
      console.warn(
        `[Validator] No factory context found for player ${player.steam_id}.`,
      );
      throw new ValidationError(
        "Factory context missing for player validation",
        500,
      );
    }
    this.runPlayerChecks(oldPlayer, player, this.oldFactoryDataCache);
  }

  getOldFactoryData(): FactoryWorldData | undefined {
    return this.oldFactoryDataCache;
  }

  getOldPlayerData(
    steam_id: string,
    factory_id: string,
  ): PlayerSessionData | undefined {
    const playerKey = `${steam_id}:${factory_id}`;
    return this.oldPlayersDataCache.get(playerKey);
  }

  private async fetchSnapshot(table: string, id: string, factoryId?: string) {
    let query = this.db.from(table).select("*");
    if (table === "factories") query = query.eq("id", id);
    else query = query.eq("steam_id", id).eq("factory_id", factoryId);

    const { data } = await query.maybeSingle();
    return data;
  }

  private runFactoryChecks(
    old: FactoryWorldData,
    incoming: Partial<FactoryWorldData>,
  ) {
    if (!old) return;

    // --- ICI TU AJOUTES TES CHECKS FACTORY ---
    // Exemple : CheckScrapRate(old, incoming);
    // Exemple : CheckPrestigeFlow(old, incoming);

    console.log(`[Validator] Factory ${incoming.id} checked.`);
  }

  private runPlayerChecks(
    old: PlayerSessionData,
    incoming: Partial<PlayerSessionData>,
    factoryContext: FactoryWorldData,
  ) {
    if (!old) return;

    // --- ICI TU AJOUTES TES CHECKS PLAYER ---
    // Exemple : CheckInventoryCoherence(old, incoming, factoryContext);

    console.log(`[Validator] Player ${incoming.steam_id} checked.`);
  }
}

// --- 4. REPOSITORY ---

class SyncRepository {
  constructor(private db: SupabaseClient) {}

  async fetchFactory(factoryId: string) {
    const { data, error } = await this.db
      .from("factories")
      .select("*")
      .eq("id", factoryId)
      .maybeSingle();

    if (error) {
      console.error(`[Repository] Error fetching factory: ${error.message}`);
      return null;
    }
    return data;
  }

  async fetchPlayer(steam_id: string, factory_id: string) {
    const { data, error } = await this.db
      .from("factory_players")
      .select("*")
      .eq("steam_id", steam_id)
      .eq("factory_id", factory_id)
      .maybeSingle();

    if (error) {
      console.error(
        `[Repository] Error fetching player ${steam_id}: ${error.message}`,
      );
      return null;
    }
    return data;
  }

  async fetchPlayersByFactory(factory_id: string) {
    const { data, error } = await this.db
      .from("factory_players")
      .select("*")
      .eq("factory_id", factory_id);

    if (error) {
      console.error(
        `[Repository] Error fetching players for factory ${factory_id}: ${error.message}`,
      );
      return [];
    }
    return data || [];
  }

  async atomicSave(
    payload: SyncPayload,
    validator: SyncValidator,
  ): Promise<void> {
    const { factory_data, players_data } = payload;

    // Save Factory first
    if (factory_data) {
      const oldFactory = validator.getOldFactoryData();
      const factoryRes = await this.saveFactory(factory_data, oldFactory);

      if (factoryRes.error) {
        throw new Error(
          `Database Error (Factory): ${factoryRes.error.message}`,
        );
      }
    }

    // Then save players in parallel
    if (players_data && players_data.length > 0) {
      const playerPromises = players_data.map((player) => {
        if (!player.steam_id || !player.factory_id) {
          console.log(
            `[Repository] Error that should have been caught by validation for player: ${player.steam_id}`,
          );
          throw new RepositoryError(
            "Error that should have been caught by validation",
            500,
          );
        }
        const oldPlayer = validator.getOldPlayerData(
          player.steam_id,
          player.factory_id,
        );
        return this.savePlayer(player, oldPlayer);
      });

      const playerResults = await Promise.all(playerPromises);

      for (const res of playerResults) {
        if (res.error) {
          throw new Error(`Database Error (Player): ${res.error.message}`);
        }
      }
    }
  }

  async saveFactory(
    factory_data: Partial<FactoryWorldData>,
    oldFactory?: FactoryWorldData,
  ) {
    const row = {
      ...factory_data,
    };

    if (oldFactory) {
      // PATCH/UPDATE - data exists in DB
      return await this.db
        .from("factories")
        .update(row)
        .eq("id", factory_data.id);
    } else {
      // UPSERT - new data or fallback
      return await this.db.from("factories").upsert(row, {
        onConflict: "id",
      });
    }
  }

  async savePlayer(
    player_data: Partial<PlayerSessionData>,
    oldPlayer?: PlayerSessionData,
  ) {
    const row = {
      ...player_data,
    };

    if (oldPlayer) {
      // PATCH/UPDATE - data exists in DB
      return await this.db
        .from("factory_players")
        .update(row)
        .eq("steam_id", player_data.steam_id)
        .eq("factory_id", player_data.factory_id);
    } else {
      // UPSERT - new data or fallback
      return await this.db.from("factory_players").upsert(row, {
        onConflict: "factory_id,steam_id",
      });
    }
  }
}

// --- 5. MAIN HANDLER ---

Deno.serve(async (req: Request) => {
  // --- CORS PRE-FLIGHT ---
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  // --- PAYLOAD PARSING ---
  try {
    if (req.method !== "POST" && req.method !== "GET") {
      throw new ValidationError("Method not allowed", 405);
    }
    let payload: SyncPayload;
    try {
      if (req.method === "POST") {
        payload = await req.json();

        // --- STRICT PAYLOAD VALIDATION ---
        if (!payload.factory_id || typeof payload.factory_id !== "string") {
          throw new ValidationError(
            "Missing or invalid 'factory_id' in payload",
            400,
          );
        }
        if (payload.factory_data && typeof payload.factory_data !== "object") {
          throw new ValidationError("Invalid 'factory_data' format", 400);
        }
        if (payload.players_data && !Array.isArray(payload.players_data)) {
          throw new ValidationError("Invalid 'players_data' format", 400);
        }
      } else {
        // For GET, we only need factory_id from query params
        const url = new URL(req.url);
        const factoryId = url.searchParams.get("factory_id");
        if (!factoryId) throw new ValidationError("Missing factory_id", 400);
        payload = { factory_id: factoryId };
      }
    } catch (error) {
      if (error instanceof ValidationError) throw error;
      throw new ValidationError("Invalid JSON payload", 400);
    }

    // --- HEADERS & DATA ---

    const sboxToken = req.headers.get("X-sbox-Auth-Token");
    const steamIdHeader = req.headers.get("X-Steam-Id");
    const serverKey = req.headers.get("X-Server-Key");
    const adminKey = req.headers.get("X-Admin-Key");

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
    );

    const repository = new SyncRepository(supabase);

    // --- AUTHENTICATION LAYER ---
    let callerSteamId: string | null = null;

    if (serverKey /*&& serverKey === Deno.env.get("INTERNAL_SERVER_KEY")*/) {
      /*console.log("[Auth] Trusted Dedicated Server detected.");*/
      throw new AuthenticationError(
        "Dedicated server access is currently disabled",
        403,
      );
    } else if (sboxToken && steamIdHeader) {
      callerSteamId = await verifySboxToken(steamIdHeader, sboxToken);
    } else if (adminKey && adminKey === Deno.env.get("ADMIN_KEY")) {
      console.log("[Auth] Admin access granted.");
      callerSteamId = steamIdHeader || "76561198254131681";
    } else {
      throw new AuthenticationError("No valid credentials provided", 401);
    }

    // --- AUTHORITY LAYER (Ownership verification) ---
    const { data: dbFactory, error: factoryFetchError } = await supabase
      .from("factories")
      .select("host_steam_id, is_sandbox")
      .eq("id", payload.factory_id)
      .maybeSingle();

    if (factoryFetchError) {
      throw new RepositoryError(
        `Failed to fetch factory: ${factoryFetchError.message}`,
        500,
      );
    }

    if (
      dbFactory && callerSteamId && dbFactory.host_steam_id !== callerSteamId
    ) {
      throw new AuthenticationError(
        "Unauthorized: You do not own this factory",
        403,
      );
    }

    // --- FETCH DATA (GET) ---
    if (req.method === "GET") {
      const [factoryRes, playersRes] = await Promise.all([
        repository.fetchFactory(payload.factory_id),
        repository.fetchPlayersByFactory(payload.factory_id),
      ]);

      return createJsonResponse(
        {
          status: "success",
          factory_data: factoryRes || null,
          players_data: playersRes || [],
        },
        200,
      );
    }

    // --- SYNC & ANTI-CHEAT LAYER (POST) ---

    if (
      !payload.factory_data &&
      (!payload.players_data || payload.players_data.length === 0)
    ) {
      return createJsonResponse({ message: "Nothing to sync" }, 200);
    }

    const validator = new SyncValidator(supabase);

    let hasJustBeenFlagged = false;

    // Determine if this is a new factory or sandbox mode
    const factoryExists = !!dbFactory;
    const isSandboxMode = dbFactory?.is_sandbox ?? false;

    console.log(
      `[SYNC] Factory ${payload.factory_id} - Exists: ${factoryExists}, Sandbox: ${isSandboxMode}`,
    );

    if (isSandboxMode) {
      console.warn(
        `[SYNC] Factory ${payload.factory_id} is in Sandbox mode. Skipping validation checks.`,
      );
      if (payload.factory_data) payload.factory_data.is_sandbox = true;
    }

    // Run anti-cheat validation only for non-sandbox, existing factories
    if (!isSandboxMode && factoryExists) {
      try {
        if (payload.factory_data) {
          await validator.validateFactory(
            payload.factory_id,
            payload.factory_data,
          );
        }

        if (payload.players_data && payload.players_data.length > 0) {
          for (const player of payload.players_data) {
            await validator.validatePlayer(payload.factory_id, player);
          }
        }
      } catch (error) {
        if (error instanceof CheatingAttemptError) {
          console.warn(
            `[ANTI-CHEAT] Factory ${payload.factory_id} flagged as Sandbox! Reason: ${error.message}`,
          );

          if (!payload.factory_data) {
            payload.factory_data = { id: payload.factory_id } as Partial<
              FactoryWorldData
            >;
          }
          payload.factory_data.is_sandbox = true;

          hasJustBeenFlagged = true;
        } else {
          throw error;
        }
      }
    }

    // --- PERSISTENCE ---
    await repository.atomicSave(payload, validator);

    // --- RESPONSE ---
    return createJsonResponse({
      status: "success",
      type: hasJustBeenFlagged ? "SandboxFlagged" : "SyncComplete",
      message: hasJustBeenFlagged
        ? "Cheat detected. Save converted to Sandbox mode."
        : "Data synchronized",
      is_sandbox: isSandboxMode || hasJustBeenFlagged,
      factory_created: !factoryExists,
    }, 200);
  } catch (error) {
    const errorMessage = error instanceof Error
      ? error.message
      : "Unknown error";
    console.error(`[Sync Error]: ${errorMessage}`);

    const status = error instanceof ValidationError
      ? error.statusCode
      : error instanceof RepositoryError
      ? error.statusCode
      : error instanceof AuthenticationError
      ? error.statusCode
      : 500;
    return createJsonResponse({
      status: "error",
      type: error instanceof Error ? error.name : "UnknownError",
      message: "An error occurred during synchronization",
      error: errorMessage,
    }, status);
  }
});
