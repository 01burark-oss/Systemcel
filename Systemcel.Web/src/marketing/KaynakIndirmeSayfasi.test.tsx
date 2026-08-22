import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { KaynakIndirmeSayfasi } from "./KaynakIndirmeSayfasi";

afterEach(cleanup);

describe("KaynakIndirmeSayfasi", () => {
  it("ilk 50 fiyatını ve doğru PDF bağlantısını gösterir", () => {
    render(<KaynakIndirmeSayfasi kod="50" />);

    expect(screen.getByText(/yıllık ₺11\.880 \+ KDV/i)).toBeInTheDocument();
    expect(screen.getByText(/yıllık ₺15\.480 \+ KDV/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /dosyayı indir/i })).toHaveAttribute(
      "href",
      "/kaynaklar/dosyalar/systemcel-ilk-50-kampanya-detaylari.pdf"
    );
  });

  it("nakit anahtarını düzenlenebilir Excel kaynağına bağlar", () => {
    render(<KaynakIndirmeSayfasi kod="nakit" />);

    expect(screen.getByText(/13 haftalık nakit akışı şablonu/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /dosyayı indir/i })).toHaveAttribute(
      "href",
      "/kaynaklar/dosyalar/systemcel-13-haftalik-nakit-akisi.xlsx"
    );
  });
});
