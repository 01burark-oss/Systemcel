export type SubscriptionAccountType = "Isletme" | "Muhasebeci";
export type SubscriptionBilling = "Aylik" | "Yillik";

const planOwners: Record<string, SubscriptionAccountType> = {
  isletme_baslangic: "Isletme",
  isletme_buyume: "Isletme",
  isletme_kurumsal: "Isletme",
  muhasebeci_standart: "Muhasebeci",
  muhasebeci_pro: "Muhasebeci"
};

export function buildSubscriptionStartHref({
  signedIn,
  accountType,
  planCode,
  billing
}: {
  signedIn: boolean;
  accountType: SubscriptionAccountType;
  planCode: string;
  billing: SubscriptionBilling;
}) {
  if (planOwners[planCode] !== accountType) {
    throw new Error("Plan hesap tipiyle uyumlu değil.");
  }

  const workspaceUrl = new URL("/app/abonelik", window.location.origin);
  workspaceUrl.searchParams.set("plan", planCode);
  workspaceUrl.searchParams.set("billing", billing);
  const workspacePath = `${workspaceUrl.pathname}${workspaceUrl.search}`;
  if (signedIn) return workspacePath;

  const authUrl = new URL("/kayit", window.location.origin);
  authUrl.searchParams.set("hesapTipi", accountType);
  authUrl.searchParams.set("returnUrl", workspacePath);
  return `${authUrl.pathname}${authUrl.search}`;
}

export function sanitizeAppReturnUrl(value: string | null | undefined) {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return "/app";

  let url: URL;
  try {
    url = new URL(value, window.location.origin);
  } catch {
    return "/app";
  }

  if (url.origin !== window.location.origin || (url.pathname !== "/app" && !url.pathname.startsWith("/app/"))) return "/app";
  const planCode = url.searchParams.get("plan");
  if (planCode && !planOwners[planCode]) return "/app";
  const billing = url.searchParams.get("billing");
  if (billing && billing !== "Aylik" && billing !== "Yillik") return "/app";
  return `${url.pathname}${url.search}${url.hash}`;
}

export function accountTypeForPlan(planCode: string) {
  return planOwners[planCode] ?? null;
}
