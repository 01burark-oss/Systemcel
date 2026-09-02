import { getAuthToken } from "../auth/authToken";

export interface EntitlementProblemDetail {
  code: "subscription_required" | "limit_reached" | "feature_not_available";
  current?: number | null;
  detail: string;
  limit?: number | null;
  limitName?: string | null;
  suggestedPlanCode?: string | null;
}

const entitlementCodes = new Set([
  "subscription_required",
  "limit_reached",
  "feature_not_available"
]);

export async function jsonOku<T>(url: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  const hasFormDataBody = init?.body instanceof FormData;
  if (!headers.has("Content-Type") && !hasFormDataBody) {
    headers.set("Content-Type", "application/json");
  }

  const token = await getAuthToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let response: Response;
  let text: string;
  try {
    response = await fetch(url, { ...init, headers });
    text = await response.text();
  } catch (error) {
    if (error && typeof error === "object" && "name" in error && error.name === "AbortError") throw error;
    throw new Error("Bağlantı kurulamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.");
  }

  const fallback = "İşlem tamamlanamadı. Lütfen tekrar deneyin.";
  if (response.status >= 500) throw new Error(fallback);
  let payload;
  try {
    payload = text ? JSON.parse(text) : null;
  } catch {
    throw new Error(fallback);
  }
  if (!response.ok) {
    if (payload?.code && entitlementCodes.has(payload.code)) {
      window.dispatchEvent(new CustomEvent<EntitlementProblemDetail>("systemcel:entitlement", {
        detail: {
          code: payload.code,
          current: payload.current,
          detail: payload.detail ?? "Bu işlem mevcut planınızda kullanılamıyor.",
          limit: payload.limit,
          limitName: payload.limitName,
          suggestedPlanCode: payload.suggestedPlanCode
        }
      }));
    }

    throw new Error(payload?.mesaj ?? payload?.detail ?? "İşlem tamamlanamadı.");
  }

  return payload as T;
}
