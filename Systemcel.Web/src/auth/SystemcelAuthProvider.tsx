import React from "react";
import { configureClerk, isClerkConfigured, loadClerkClient, type ClerkJsClient, type ClerkRuntimeConfig, type ClerkUserSummary } from "./clerkClient";
import { setAuthTokenGetter } from "./authToken";

interface SystemcelAuthState {
  clerkEnabled: boolean;
  isLoaded: boolean;
  isSignedIn: boolean;
  user: ClerkUserSummary | null;
  clerk: ClerkJsClient | null;
  error: string;
}

const disabledAuthState: SystemcelAuthState = {
  clerkEnabled: false,
  isLoaded: true,
  isSignedIn: true,
  user: null,
  clerk: null,
  error: ""
};

const loadingAuthState: SystemcelAuthState = {
  clerkEnabled: true,
  isLoaded: false,
  isSignedIn: false,
  user: null,
  clerk: null,
  error: ""
};

const SystemcelAuthContext = React.createContext<SystemcelAuthState>(disabledAuthState);

export function SystemcelAuthProvider({ children }: { children: React.ReactNode }) {
  const [authState, setAuthState] = React.useState<SystemcelAuthState>(loadingAuthState);

  React.useEffect(() => {
    let disposed = false;
    let unsubscribe: (() => void) | undefined;

    async function initializeAuth() {
      try {
        const config = await fetchRuntimeConfig();
        if (disposed) {
          return;
        }

        configureClerk(config.clerk);
        if (!isClerkConfigured()) {
          setAuthTokenGetter(null);
          setAuthState(config.clerk?.enabled
            ? {
                clerkEnabled: true,
                isLoaded: true,
                isSignedIn: false,
                user: null,
                clerk: null,
                error: "Oturum anahtari tanimli degil."
              }
            : disabledAuthState);
          return;
        }

        const clerk = await loadClerkClient();
        if (disposed) {
          return;
        }

        const syncState = () => {
          const isSignedIn = Boolean(clerk.isSignedIn);
          setAuthTokenGetter(isSignedIn && clerk.session ? () => clerk.session!.getToken() : null);
          setAuthState({
            clerkEnabled: true,
            isLoaded: true,
            isSignedIn,
            user: clerk.user ?? null,
            clerk,
            error: ""
          });
        };

        unsubscribe = clerk.addListener?.(() => syncState());
        syncState();
      } catch (error) {
        if (disposed) {
          return;
        }

        setAuthTokenGetter(null);
        setAuthState({
          clerkEnabled: true,
          isLoaded: true,
          isSignedIn: false,
          user: null,
          clerk: null,
          error: error instanceof Error ? error.message : "Oturum ayarlari yuklenemedi."
        });
      }
    }

    initializeAuth();

    return () => {
      disposed = true;
      unsubscribe?.();
      setAuthTokenGetter(null);
    };
  }, []);

  return <SystemcelAuthContext.Provider value={authState}>{children}</SystemcelAuthContext.Provider>;
}

export function useSystemcelAuth() {
  return React.useContext(SystemcelAuthContext);
}

interface SystemcelRuntimeConfig {
  clerk?: ClerkRuntimeConfig | null;
}

async function fetchRuntimeConfig(): Promise<SystemcelRuntimeConfig> {
  const response = await fetch("/api/public/config", {
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new Error("Oturum ayarlari alinamadi.");
  }

  return response.json() as Promise<SystemcelRuntimeConfig>;
}
