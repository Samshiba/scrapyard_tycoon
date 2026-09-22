import { AuthenticationError } from "./errors.ts";

export async function verifySboxToken(
    steamId: string,
    token: string,
): Promise<string> {
    const response = await fetch(
        "https://services.facepunch.com/sbox/auth/token",
        {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                steamid: steamId,
                token: token,
            }),
        },
    );

    if (!response.ok) {
        throw new AuthenticationError(
            `Facepunch API error: ${response.status} ${response.statusText}`,
            401,
        );
    }

    const rawText = await response.text();

    const safeJsonText = rawText.replace(/:\s*(\d{15,})/g, ':"$1"');

    const result = JSON.parse(safeJsonText);

    const status = result.Status || result.status;
    const returnedSteamId = (result.SteamId || result.steamId)?.toString();

    console.log(
        `[Auth] Facepunch Verified - Status: ${status}, SteamID: ${returnedSteamId}`,
    );

    if (status !== "ok" || returnedSteamId !== steamId) {
        throw new AuthenticationError(
            `Auth failed. Identity mismatch or invalid token.`,
            401,
        );
    }

    return steamId;
}
