import { describe, expect, it } from "vitest";
import { cariHedefId } from "./CariHesaplarSayfasi";

describe("cariHedefId", () => {
  it("ilk yüklemede mevcut ilk kartı kendiliğinden düzenlemeye açmaz", () => {
    expect(cariHedefId(undefined, null)).toBeNull();
  });

  it("kullanıcının seçili kartını yenileme sırasında korur", () => {
    expect(cariHedefId(undefined, 42)).toBe(42);
  });

  it("açık tercihi ve yeni kart isteğini uygular", () => {
    expect(cariHedefId(7, 42)).toBe(7);
    expect(cariHedefId(null, 42)).toBeNull();
  });
});
