import React from "react";
import { ArrowRight, ShieldCheck } from "lucide-react";
import systemcelIcon from "../assets/systemcel-icon.png";
import { useSystemcelAuth } from "./SystemcelAuthProvider";

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const auth = useSystemcelAuth();

  if (!auth.clerkEnabled) {
    return <>{children}</>;
  }

  if (!auth.isLoaded) {
    return <AuthStatus title="Oturum hazırlanıyor" text="Systemcel hesabın kontrol ediliyor." />;
  }

  if (auth.error) {
    return (
      <AuthStatus
        title="Oturum bağlantısı hazır değil"
        text={auth.error}
        actionHref="/giris"
        actionText="Giriş ekranına git"
      />
    );
  }

  if (!auth.isSignedIn) {
    const returnUrl = encodeURIComponent(`${window.location.pathname}${window.location.search}`);
    return (
      <AuthStatus
        title="Devam etmek için giriş yap"
        text="Çalışma alanı Systemcel hesabına bağlı olacak. Önce oturum aç, sonra kaldığın yerden devam et."
        actionHref={`/giris?returnUrl=${returnUrl}`}
        actionText="Giriş yap"
        secondaryHref="/kayit"
        secondaryText="Kayıt ol"
      />
    );
  }

  return <>{children}</>;
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
          <span className="auth-shell__brand-mark">
            <img src={systemcelIcon} alt="" />
          </span>
          <span className="auth-shell__brand-copy">
            <strong>SYSTEMCEL</strong>
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
