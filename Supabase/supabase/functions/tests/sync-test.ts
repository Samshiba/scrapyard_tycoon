import {
    assertEquals,
    assertStringIncludes,
} from "https://deno.land/std@0.208.0/assert/mod.ts";
// Import optionnel pour faciliter les tests BDD (describe, it)
import {
    afterAll,
    beforeAll,
    describe,
    it,
} from "https://deno.land/std@0.208.0/testing/bdd.ts";

const FUNCTION_URL = "http://127.0.0.1:54321/functions/v1/sync"; // URL locale de ta fonction
const TEST_STEAM_ID_OWNER = "76561198000000001";
const TEST_STEAM_ID_HACKER = "76561198000000002";
const TEST_FACTORY_ID = "test_factory_001";

// --- MOCKING ---
// Pour éviter de spammer la vraie API Steam pendant les tests,
// on intercepte les appels 'fetch' sortants de la fonction.
const originalFetch = globalThis.fetch;

describe("Sync Edge Function - Test Suite", () => {
    beforeAll(() => {
        // Remplacement temporaire de fetch pour simuler l'API Steam
        globalThis.fetch = async (
            input: RequestInfo | URL,
            init?: RequestInit,
        ) => {
            const url = input.toString();

            if (url.includes("api.steampowered.com")) {
                // Simulation d'un ticket valide pour le propriétaire
                if (url.includes("valid_owner_ticket")) {
                    return new Response(
                        JSON.stringify({
                            response: {
                                params: { steamid: TEST_STEAM_ID_OWNER },
                            },
                        }),
                    );
                }
                // Simulation d'un ticket valide mais pour un hacker
                if (url.includes("valid_hacker_ticket")) {
                    return new Response(
                        JSON.stringify({
                            response: {
                                params: { steamid: TEST_STEAM_ID_HACKER },
                            },
                        }),
                    );
                }
                // Ticket invalide
                return new Response(
                    JSON.stringify({
                        response: { error: { errordesc: "Invalid ticket" } },
                    }),
                );
            }

            // Laisser passer les appels normaux (vers Supabase local)
            return originalFetch(input, init);
        };
    });

    afterAll(() => {
        // Restauration du fetch original
        globalThis.fetch = originalFetch;
    });

    // ==========================================
    // 1. TESTS DE SÉCURITÉ & AUTHENTIFICATION
    // ==========================================

    it("1.1. Doit autoriser les requêtes CORS (OPTIONS)", async () => {
        const req = await fetch(FUNCTION_URL, { method: "OPTIONS" });
        assertEquals(req.status, 200);
        assertEquals(req.headers.get("Access-Control-Allow-Origin"), "*");
    });

    it("1.2. Doit rejeter une requête sans Steam Ticket (401)", async () => {
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            body: JSON.stringify({ factory_id: TEST_FACTORY_ID }),
        });
        const res = await req.json();

        assertEquals(req.status, 401);
        assertEquals(res.type, "AuthenticationError");
    });

    it("1.3. Doit rejeter un Dedicated Server (403 - Temporairement désactivé)", async () => {
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Server-Key": "any_key" },
            body: JSON.stringify({ factory_id: TEST_FACTORY_ID }),
        });
        const res = await req.json();

        assertEquals(req.status, 403);
        assertStringIncludes(res.message, "disabled");
    });

    it("1.4. Doit rejeter si l'Hôte n'est pas le propriétaire de l'usine (403)", async () => {
        // Note: Cela suppose que la factory "test_factory_001" existe en DB locale et appartient à TEST_STEAM_ID_OWNER
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_hacker_ticket" },
            body: JSON.stringify({ factory_id: TEST_FACTORY_ID }),
        });
        const res = await req.json();

        assertEquals(req.status, 403);
        assertStringIncludes(res.message, "You do not own this factory");
    });

    // ==========================================
    // 2. TESTS DE VALIDATION DU PAYLOAD
    // ==========================================

    it("2.1. Doit retourner 200 (Nothing to sync) si le payload est vide", async () => {
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify({ factory_id: TEST_FACTORY_ID }),
        });
        const res = await req.json();

        assertEquals(req.status, 200);
        assertEquals(res.message, "Nothing to sync");
    });

    it("2.2. Doit rejeter si factory_data.id ne correspond pas au factory_id racine (400)", async () => {
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify({
                factory_id: TEST_FACTORY_ID,
                factory_data: { id: "hacked_factory_id", current_scrap: 999 },
            }),
        });
        const res = await req.json();

        assertEquals(req.status, 400);
        assertEquals(res.type, "ValidationError");
        assertStringIncludes(res.message, "factory_id mismatch");
    });

    it("2.3. Doit rejeter si un player tente de polluer une autre usine (400)", async () => {
        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify({
                factory_id: TEST_FACTORY_ID,
                players_data: [{
                    steam_id: "123",
                    factory_id: "other_factory",
                }],
            }),
        });
        const res = await req.json();

        assertEquals(req.status, 400);
        assertStringIncludes(res.message, "mismatch");
    });

    // ==========================================
    // 3. TESTS HEURISTIQUES / ANTI-CHEAT
    // ==========================================

    it("3.1. Doit rejeter une sauvegarde trop rapide (Rate Limit / Spam Anti-Cheat - 429)", async () => {
        const payload = {
            factory_id: TEST_FACTORY_ID,
            factory_data: { id: TEST_FACTORY_ID, current_scrap: 100 },
        };

        // Première sauvegarde (devrait passer)
        await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify(payload),
        });

        // Seconde sauvegarde IMMÉDIATEMENT (devrait être flaggé 429 Rate Limit)
        const req2 = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify(payload),
        });
        const res2 = await req2.json();

        assertEquals(req2.status, 429);
        assertStringIncludes(res2.message, "Rate limit");
    });

    // ==========================================
    // 4. TESTS DE SUCCÈS (Le "Happy Path")
    // ==========================================

    it("4.1. Doit réussir une sauvegarde complète (Factory + Players)", async () => {
        // On attend 1.1 seconde pour passer le Rate Limit du test précédent
        await new Promise((resolve) => setTimeout(resolve, 1100));

        const req = await fetch(FUNCTION_URL, {
            method: "POST",
            headers: { "X-Steam-Ticket": "valid_owner_ticket" },
            body: JSON.stringify({
                factory_id: TEST_FACTORY_ID,
                factory_data: {
                    id: TEST_FACTORY_ID,
                    current_scrap: 500, // PATCH Update
                },
                players_data: [
                    {
                        steam_id: "111",
                        factory_id: TEST_FACTORY_ID,
                        vip_status: true,
                    },
                ],
            }),
        });

        const res = await req.json();

        assertEquals(req.status, 200);
        assertEquals(res.status, "success");
        assertEquals(res.type, "SyncComplete");
    });
});
