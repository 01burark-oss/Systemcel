import React from "react";
import { ArrowRight, ShieldCheck } from "lucide-react";
import { useSystemcelAuth } from "./SystemcelAuthProvider";

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const auth = useSystemcelAuth();

  React.useEffect(() => {
    if (!auth.clerkEnabled || !auth.isLoaded || auth.isSignedIn || auth.error) {
      return;
    }

    const returnUrl = encodeURIComponent(`${window.location.pathname}${window.location.search}`);
    window.location.replace(`/giris?returnUrl=${returnUrl}`);
  }, [auth.clerkEnabled, auth.isLoaded, auth.isSignedIn, auth.error]);

  if (!auth.clerkEnabled) {
    return <>{children}</>;
  }

  if (!auth.isLoaded) {
    return <WorkspaceLoadingScreen />;
  }

  if (auth.error) {
    return <WorkspaceLoadingScreen error={auth.error} />;
  }

  if (!auth.isSignedIn) {
    return <WorkspaceLoadingScreen />;
  }

  return <>{children}</>;
}

function WorkspaceLoadingScreen({ error }: { error?: string }) {
  return (
    <main className={`workspace-loading${error ? " workspace-loading--error" : ""}`} role={error ? "alert" : "status"} aria-live={error ? "assertive" : "polite"}>
      <div className="workspace-loading__grid" aria-hidden="true" />
      <section className="workspace-loading__card">
        <div className="workspace-loading__brand">
          <span className="workspace-loading__mark" aria-hidden="true"><i /><i /><i /><i /></span>
          <span><strong>systemcel</strong><small>Finance Suite</small></span>
        </div>
        <div className="workspace-loading__orbit" aria-hidden="true">
          <span className="workspace-loading__orbit-ring" />
          <span className="workspace-loading__orbit-core"><i /><i /><i /><i /></span>
        </div>
        <p className="workspace-loading__eyebrow">SYSTEMCEL / ÇALIŞMA ALANI</p>
        <h1>{error ? "Çalışma alanı açılamadı" : "Çalışma alanın yükleniyor"}</h1>
        <p className="workspace-loading__description">{error || "Güvenli oturumun hazırlanıyor. Birazdan kaldığın yerden devam edeceksin."}</p>
        {error ? <a className="workspace-loading__action" href="/giris">Giriş ekranına git <ArrowRight size={18} /></a> : (
          <div className="workspace-loading__progress" aria-hidden="true"><span /></div>
        )}
      </section>
    </main>
  );
}

export function AuthStatus({
  title,
  text,
  actionHref,
  actionText,
  secondaryHref,
  secondaryText
}: {
  title: string;
  text: string;
  actionHref?: string;
  actionText?: string;
  secondaryHref?: string;
  secondaryText?: string;
}) {
  return (
    <main className="auth-shell">
      <section className="auth-shell__panel auth-shell__panel--compact">
        <a className="auth-shell__brand" href="/" aria-label="Systemcel">
          <span className="auth-shell__brand-mark auth-shell__brand-mark--tiles" aria-hidden="true">
            <i />
            <i />
            <i />
            <i />
          </span>
          <span className="auth-shell__brand-copy">
            <strong>systemcel</strong>
            <small>Finance Suite</small>
          </span>
        </a>
        <div className="auth-shell__icon">
          <ShieldCheck size={30} />
        </div>
        <h1>{title}</h1>
        <p>{text}</p>
        {actionHref && actionText ? (
          <div className="auth-shell__actions">
            <a className="auth-shell__button auth-shell__button--primary" href={actionHref}>
              {actionText}
              <ArrowRight size={18} />
            </a>
            {secondaryHref && secondaryText ? (
              <a className="auth-shell__button" href={secondaryHref}>
                {secondaryText}
              </a>
            ) : null}
          </div>
        ) : null}
      </section>
    </main>
  );
}
