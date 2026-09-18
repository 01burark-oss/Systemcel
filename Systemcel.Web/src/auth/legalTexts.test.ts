import { describe, expect, it } from "vitest";
import { legalTexts, publicBusinessIdentity } from "./legalTexts";

describe("subscription legal text", () => {
  it.each(["tr", "en"] as const)("keeps the cancellation and renewal terms explicit in %s", (language) => {
    const content = legalTexts[language].subscription;
    const searchable = [content.intro, content.note, ...content.sections.flatMap((section) => [section.title, section.text])]
      .join(" ")
      .toLocaleLowerCase(language === "tr" ? "tr-TR" : "en-US");

    if (language === "tr") {
      expect(searchable).toContain("aylık");
      expect(searchable).toContain("dönem sonu iptal");
      expect(searchable).toContain("emredici");
    } else {
      expect(searchable).toContain("monthly");
      expect(searchable).toContain("paid period ends");
      expect(searchable).toContain("mandatory");
    }
  });

  it("uses the verified public business identity without stale placeholders", () => {
    const searchable = JSON.stringify(legalTexts);

    expect(searchable).toContain(publicBusinessIdentity.tr.tax);
    expect(searchable).toContain(publicBusinessIdentity.tr.address);
    expect(searchable).not.toContain("Cemalbey");
    expect(searchable).not.toMatch(/\[(?:VERGİ|AÇIK ADRES|RESMÎ|FULL ADDRESS|TAX AND REGISTRY|OFFICIAL EMAIL)/);
  });
});
