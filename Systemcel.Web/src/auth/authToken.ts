type AuthTokenGetter = () => Promise<string | null>;

let authTokenGetter: AuthTokenGetter | null = null;

export function setAuthTokenGetter(getter: AuthTokenGetter | null) {
  authTokenGetter = getter;
}

export async function getAuthToken() {
  if (!authTokenGetter) {
    return null;
  }

  try {
    return await authTokenGetter();
  } catch {
    return null;
  }
}
