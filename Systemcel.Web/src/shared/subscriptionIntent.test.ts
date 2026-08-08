import { describe, expect, it } from "vitest";
import { buildSubscriptionStartHref, sanitizeAppReturnUrl } from "./subscriptionIntent";

describe("subscription intent", () => {
  it("preserves account, plan and billing through registration", () => {
    const href = buildSubscriptionStartHref({ signedIn: false, accountType: "Muhasebeci", planCode: "muhasebeci_standart", billing: "Yillik" });
    const auth = new URL(href, window.location.origin);
    expect(auth.pathname).toBe("/kayit");
    expect(auth.searchParams.get("hesapTipi")).toBe("Muhasebeci");
    expect(auth.searchParams.get("returnUrl")).toBe("/app/abonelik?plan=muhasebeci_standart&billing=Yillik");
  });

  it("sends signed-in users directly to the selected plan", () => {
    expect(buildSubscriptionStartHref({ signedIn: true, accountType: "Isletme", planCode: "isletme_buyume", billing: "Aylik" }))
      .toBe("/app/abonelik?plan=isletme_buyume&billing=Aylik");
  });

  it("rejects cross-role plans and unsafe return URLs", () => {
    expect(() => buildSubscriptionStartHref({ signedIn: false, accountType: "Isletme", planCode: "muhasebeci_pro", billing: "Aylik" })).toThrow(/hesap tipi/i);
    expect(sanitizeAppReturnUrl("//attacker.example/path")).toBe("/app");
    expect(sanitizeAppReturnUrl("/app/abonelik?plan=unknown&billing=Aylik")).toBe("/app");
    expect(sanitizeAppReturnUrl("/app/abonelik?plan=isletme_buyume&billing=Yillik")).toBe("/app/abonelik?plan=isletme_buyume&billing=Yillik");
  });
});
