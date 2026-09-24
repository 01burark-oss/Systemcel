import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, expect, it, vi } from "vitest";
import { AiAssistantPanel } from "./AiAssistantPanel";
import { jsonOku } from "./json";

vi.mock("./json", () => ({ jsonOku: vi.fn() }));

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

it("sends the server-issued conversation token with a short follow-up", async () => {
  const user = userEvent.setup();
  const requests: Array<Record<string, unknown>> = [];
  vi.mocked(jsonOku).mockImplementation(async (url, init) => {
    if (url === "/api/ai/durum")
      return { configured: true, usage: { aiAktif: true, sinirsizPlan: true } };
    if (url === "/api/ai/sohbet") {
      requests.push(JSON.parse(String(init?.body)) as Record<string, unknown>);
      return requests.length === 1
        ? { answer: "Vadesi geçmiş tahsilatları önceleyin.", continuationToken: "sealed-context" }
        : { answer: "Önce iki müşteriye ödeme planı gönderin.", continuationToken: "next-context" };
    }
    throw new Error(`Unexpected request: ${url}`);
  });

  render(<AiAssistantPanel />);
  await user.click(screen.getByRole("button", { name: "AI asistanını aç" }));
  await user.click(screen.getByRole("button", { name: "Sohbet" }));

  await user.type(screen.getByPlaceholderText("Giderleri nasıl düşürebiliriz?"), "Tahsilatlarım nasıl?");
  await user.click(screen.getByRole("button", { name: "Mesaj gönder" }));
  await screen.findByText("Vadesi geçmiş tahsilatları önceleyin.");

  await user.type(screen.getByPlaceholderText("Giderleri nasıl düşürebiliriz?"), "ne yapmalıyım peki");
  await user.click(screen.getByRole("button", { name: "Mesaj gönder" }));
  await screen.findByText("Önce iki müşteriye ödeme planı gönderin.");

  await waitFor(() => expect(requests).toHaveLength(2));
  expect(requests[0].continuationToken).toBeNull();
  expect(requests[1].continuationToken).toBe("sealed-context");
  expect(requests[1].mesaj).toBe("ne yapmalıyım peki");
});
