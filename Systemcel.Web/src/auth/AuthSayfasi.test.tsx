import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthSayfasi, OAuthCallbackSayfasi } from "./AuthSayfasi";
import { useSystemcelAuth } from "./SystemcelAuthProvider";
import type { ClerkJsClient } from "./clerkClient";

vi.mock("./SystemcelAuthProvider", () => ({
  useSystemcelAuth: vi.fn()
}));

function createClerk() {
  const signInCreate = vi.fn();
  const signUpCreate = vi.fn();
  const prepareSecondFactor = vi.fn().mockResolvedValue({});
  const handleRedirectCallback = vi.fn().mockResolvedValue(undefined);

  const clerk = {
    client: {
      signIn: {
        create: signInCreate,
        attemptFirstFactor: vi.fn(),
        prepareSecondFactor,
        attemptSecondFactor: vi.fn(),
        authenticateWithRedirect: vi.fn()
      },
      signUp: {
        create: signUpCreate,
        prepareEmailAddressVerification: vi.fn(),
        attemptEmailAddressVerification: vi.fn(),
        authenticateWithRedirect: vi.fn()
      }
    },
    load: vi.fn(),
    handleRedirectCallback,
    setActive: vi.fn()
  } as unknown as ClerkJsClient;

  return { clerk, handleRedirectCallback, prepareSecondFactor, signInCreate, signUpCreate };
}

function useSignedOutClerk(clerk: ClerkJsClient) {
  vi.mocked(useSystemcelAuth).mockReturnValue({
    clerkEnabled: true,
    isLoaded: true,
    isSignedIn: false,
    user: null,
    clerk,
    error: ""
  });
}

describe("authentication routing", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.history.replaceState(null, "", "/");
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("lets the OAuth callback move between sign-in and sign-up", async () => {
    const { clerk, handleRedirectCallback } = createClerk();
    useSignedOutClerk(clerk);
    window.history.replaceState(null, "", "/oauth-callback?hesapTipi=Isletme&returnUrl=%2Fapp");

    render(<OAuthCallbackSayfasi />);

    await waitFor(() => expect(handleRedirectCallback).toHaveBeenCalledTimes(1));
    const options = handleRedirectCallback.mock.calls[0][0] as Record<string, unknown>;

    expect(options.transferable).toBe(true);
    expect(new URL(String(options.signInUrl)).pathname).toBe("/giris");
    expect(new URL(String(options.signUpUrl)).pathname).toBe("/kayit");
    expect(options.continueSignUpUrl).toBe(options.signUpUrl);
  });

  it("signs in automatically when registration finds an existing account", async () => {
    const { clerk, prepareSecondFactor, signInCreate, signUpCreate } = createClerk();
    useSignedOutClerk(clerk);
    window.history.replaceState(null, "", "/kayit?hesapTipi=Isletme");

    signUpCreate.mockRejectedValue({ errors: [{ code: "form_identifier_exists" }] });
    signInCreate.mockResolvedValue({
      status: "needs_second_factor",
      supportedSecondFactors: [{ strategy: "email_code", emailAddressId: "email_1" }]
    });

    render(<AuthSayfasi mode="sign-up" />);
    fireEvent.change(screen.getByLabelText("Ad soyad"), { target: { value: "Ayşe Yılmaz" } });
    fireEvent.change(screen.getByLabelText("E-posta"), { target: { value: "ayse@example.com" } });
    fireEvent.change(screen.getByLabelText("Parola"), { target: { value: "guvenli-parola" } });
    fireEvent.click(screen.getByRole("button", { name: "Hesap oluştur" }));

    await waitFor(() => expect(signInCreate).toHaveBeenCalledWith({
      identifier: "ayse@example.com",
      password: "guvenli-parola"
    }));
    expect(prepareSecondFactor).toHaveBeenCalledWith({
      strategy: "email_code",
      emailAddressId: "email_1"
    });
    expect(await screen.findByText("E-postanızı doğrulayın")).toBeVisible();
  });
});
