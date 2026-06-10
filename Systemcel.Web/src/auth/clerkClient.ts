export interface ClerkUserSummary {
  id: string;
  fullName?: string | null;
  username?: string | null;
  primaryEmailAddress?: {
    emailAddress?: string | null;
  } | null;
  imageUrl?: string | null;
}

export interface ClerkSession {
  getToken(options?: { template?: string }): Promise<string | null>;
}

export interface ClerkFactor {
  strategy?: string;
  emailAddressId?: string;
}

export interface ClerkAuthAttempt {
  status?: string | null;
  createdSessionId?: string | null;
  supportedSecondFactors?: ClerkFactor[] | null;
}

export interface ClerkSignInResource {
  create(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  attemptFirstFactor(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  prepareSecondFactor(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  attemptSecondFactor(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  authenticateWithRedirect(params: Record<string, unknown>): Promise<unknown>;
}

export interface ClerkSignUpResource {
  create(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  prepareEmailAddressVerification(params?: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  attemptEmailAddressVerification(params: Record<string, unknown>): Promise<ClerkAuthAttempt>;
  authenticateWithRedirect(params: Record<string, unknown>): Promise<unknown>;
}

export interface ClerkClientResource {
  signIn: ClerkSignInResource;
  signUp: ClerkSignUpResource;
}

export interface ClerkResourceEmission {
  user?: ClerkUserSummary | null;
  session?: ClerkSession | null;
}

export interface ClerkJsClient {
  isSignedIn?: boolean;
  user?: ClerkUserSummary | null;
  session?: ClerkSession | null;
  client: ClerkClientResource;
  load(options?: Record<string, unknown>): Promise<void>;
  setActive(options: { session?: string | null }): Promise<void>;
  signOut?(options?: { redirectUrl?: string }): Promise<void> | void;
  addListener?(
    callback: (emission: ClerkResourceEmission) => void,
    options?: { skipInitialEmit?: boolean }
  ): () => void;
}

declare global {
  interface Window {
    Clerk?: ClerkJsClient;
  }
}

let clerkPromise: Promise<ClerkJsClient> | null = null;
let clerkRuntimeConfig: ClerkRuntimeConfig = {
  enabled: false,
  publishableKey: "",
  jsUrl: ""
};

export interface ClerkRuntimeConfig {
  enabled: boolean;
  publishableKey: string;
  jsUrl?: string | null;
}

export function configureClerk(config: Partial<ClerkRuntimeConfig> | null | undefined) {
  const nextConfig = {
    enabled: Boolean(config?.enabled),
    publishableKey: config?.publishableKey?.trim() ?? "",
    jsUrl: config?.jsUrl?.trim() ?? ""
  };

  if (
    nextConfig.enabled !== clerkRuntimeConfig.enabled ||
    nextConfig.publishableKey !== clerkRuntimeConfig.publishableKey ||
    nextConfig.jsUrl !== clerkRuntimeConfig.jsUrl
  ) {
    clerkPromise = null;
  }

  clerkRuntimeConfig = nextConfig;
}

export function getClerkPublishableKey() {
  return clerkRuntimeConfig.enabled ? clerkRuntimeConfig.publishableKey : "";
}

export function isClerkConfigured() {
  return getClerkPublishableKey().length > 0;
}

export function loadClerkClient() {
  if (!clerkPromise) {
    clerkPromise = loadClerkClientInternal();
  }

  return clerkPromise;
}

async function loadClerkClientInternal() {
  const publishableKey = getClerkPublishableKey();
  if (!publishableKey) {
    throw new Error("Oturum anahtarı tanımlı değil.");
  }

  const clerkDomain = deriveClerkDomain(publishableKey);
  const jsUrl = clerkRuntimeConfig.jsUrl || `https://${clerkDomain}/npm/@clerk/clerk-js@6/dist/clerk.browser.js`;

  await loadScript("systemcel-session-js", jsUrl, {
    "data-clerk-publishable-key": publishableKey
  });

  if (!window.Clerk) {
    throw new Error("Oturum servisi yüklendi ama istemci bulunamadı.");
  }

  const restoreConsole = silenceSessionProviderWarnings();
  try {
    await window.Clerk.load({
      appearance: {
        options: {
          unsafe_disableDevelopmentModeWarnings: true
        }
      }
    });
  } finally {
    restoreConsole();
  }

  return window.Clerk;
}

function deriveClerkDomain(publishableKey: string) {
  const parts = publishableKey.split("_");
  if (parts.length < 3 || !parts[2]) {
    throw new Error("Oturum anahtarı formatı geçersiz.");
  }

  const decoded = atob(parts[2]);
  return decoded.endsWith("$") ? decoded.slice(0, -1) : decoded;
}

function loadScript(id: string, src: string, attributes: Record<string, string> = {}) {
  const existing = document.getElementById(id) as HTMLScriptElement | null;
  if (existing) {
    return Promise.resolve();
  }

  return new Promise<void>((resolve, reject) => {
    const script = document.createElement("script");
    script.id = id;
    script.src = src;
    script.async = true;
    script.defer = true;
    script.crossOrigin = "anonymous";
    script.type = "text/javascript";

    Object.entries(attributes).forEach(([key, value]) => {
      script.setAttribute(key, value);
    });

    script.onload = () => resolve();
    script.onerror = () => reject(new Error(`Script yuklenemedi: ${src}`));
    document.head.appendChild(script);
  });
}

function silenceSessionProviderWarnings() {
  const originalWarn = console.warn;

  console.warn = (...args: unknown[]) => {
    const message = args.map((arg) => (typeof arg === "string" ? arg : "")).join(" ");
    if (/Clerk:|clerk\.com\/docs|development keys|structural_css_pin_clerk_ui/i.test(message)) {
      return;
    }

    originalWarn(...args);
  };

  return () => {
    console.warn = originalWarn;
  };
}
