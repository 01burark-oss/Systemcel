import { expect, it } from "vitest";
import { projeksiyonOlcegi, projeksiyonCizgisi } from "./helpers";
import type { NakitProjeksiyonHaftasi } from "./types";

const weeks = (balances: number[]) => balances.map(kapanisBakiyesi => ({ kapanisBakiyesi }) as NakitProjeksiyonHaftasi);

it("keeps an empty or zero cash projection at the baseline without a negative area", () => {
  expect(projeksiyonOlcegi([]).sifirYuzde).toBe(100);
  expect(projeksiyonOlcegi(weeks([0, 0, 0])).sifirYuzde).toBe(100);
  expect(projeksiyonCizgisi(weeks([0, 0]), 100, 100)).toBe("25.0,100.0 75.0,100.0");
});

it("preserves negative and mixed balance scales", () => {
  expect(projeksiyonOlcegi(weeks([-100, -50])).sifirYuzde).toBe(0);
  expect(projeksiyonOlcegi(weeks([-100, 100])).sifirYuzde).toBe(50);
  expect(projeksiyonOlcegi(weeks([50, 100])).sifirYuzde).toBe(100);
});
