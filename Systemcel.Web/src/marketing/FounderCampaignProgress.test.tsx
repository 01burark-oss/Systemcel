import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FounderCampaignProgress } from "./FounderCampaignProgress";

describe("FounderCampaignProgress", () => {
  it("başarılı ödemelerden gelen gerçek doluluk değerini gösterir", () => {
    render(<FounderCampaignProgress total={50} won={17} percentage={34} language="tr" />);

    expect(screen.getByText("17/50 doldu")).toBeVisible();
    expect(screen.getByText("%34")).toBeVisible();
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "17");
  });

  it("sunucudan gelen sınır dışı değerleri güvenli aralığa çeker", () => {
    render(<FounderCampaignProgress total={50} won={52} percentage={104} language="en" />);

    expect(screen.getByText("50/50 filled")).toBeVisible();
    expect(screen.getByText("%100")).toBeVisible();
  });
});
