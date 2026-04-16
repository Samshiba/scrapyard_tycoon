// Setup type definitions for built-in Supabase Runtime APIs
import "@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "jsr:@supabase/supabase-js@2";
import { verifySboxToken } from "../_shared/auth.ts";
import { AuthenticationError } from "../_shared/errors.ts";
import { corsHeaders, createJsonResponse } from "../_shared/cors.ts";

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    if (req.method !== "GET") {
      return createJsonResponse({ error: "Method not allowed" }, 405);
    }

    const sboxToken = req.headers.get("X-sbox-Auth-Token");
    const steamIdHeader = req.headers.get("X-Steam-Id");
    const serverKey = req.headers.get("X-Server-Key");

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
    } else {
      throw new AuthenticationError("No valid credentials provided", 401);
    }

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
    );

    const { data: factories, error } = await supabase
      .from("factories")
      .select("*")
      .eq("host_steam_id", callerSteamId);

    if (error) throw new Error(error.message);

    return createJsonResponse(
      {
        status: "success",
        message: "Factories retrieved successfully",
        factories: factories || [],
      },
      200,
    );
  } catch (error) {
    const errorMessage = error instanceof Error
      ? error.message
      : "Unknown error";
    console.error(`[List Factories Error]: ${errorMessage}`);

    const status = error instanceof AuthenticationError
      ? error.statusCode
      : 500;
    return createJsonResponse({
      status: "error",
      type: error instanceof Error ? error.name : "UnknownError",
      message: "An error occurred during factory retrieval.",
      error: errorMessage,
    }, status);
  }
});
