import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "./json";

vi.mock("../auth/authToken", () => ({ getAuthToken: async () => null }));
afterEach(() => vi.unstubAllGlobals());

describe("user-facing request errors", () => {
  it.each([
    [500, JSON.stringify({ detail: "NpgsqlException: SELECT * FROM private_table" })],
    [502, "<html>Bad Gateway</html>"],
    [200, "<html>Unexpected proxy response</html>"]
  ])("hides server and malformed response details (%s)", async (status, body) => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(body, { status })));
    await expect(jsonOku("/test")).rejects.toThrow("İşlem tamamlanamadı. Lütfen tekrar deneyin.");
  });
  it("localizes network failures", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));
    await expect(jsonOku("/test")).rejects.toThrow("Bağlantı kurulamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.");
  });
  it("preserves actionable validation", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ mesaj: "Tutar sıfırdan büyük olmalı." }), { status: 400 })));
    await expect(jsonOku("/test")).rejects.toThrow("Tutar sıfırdan büyük olmalı.");
  });
  it("preserves cancellation", async () => {
    const error = new DOMException("Aborted", "AbortError");
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(error));
    await expect(jsonOku("/test")).rejects.toBe(error);
  });
});
