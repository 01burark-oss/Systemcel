import React from "react";
import { ChevronUp, LogOut, Mail, User, X } from "lucide-react";
import { useSystemcelAuth } from "./SystemcelAuthProvider";

function getInitials(name?: string | null, email?: string | null) {
  const source = (name || email || "S").trim();
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toLocaleUpperCase("tr-TR");
  }

  return source.slice(0, 1).toLocaleUpperCase("tr-TR");
}

export function AuthUserButton() {
  const auth = useSystemcelAuth();
  const [menuOpen, setMenuOpen] = React.useState(false);
  const [profileOpen, setProfileOpen] = React.useState(false);
  const rootRef = React.useRef<HTMLDivElement | null>(null);

  const userName = auth.user?.fullName?.trim() || "Systemcel kullanıcısı";
  const email = auth.user?.primaryEmailAddress?.emailAddress?.trim() || "";
  const username = auth.user?.username?.trim() || (email ? email.split("@")[0] : "");
  const userHandle = username ? `@${username}` : email;
  const initials = getInitials(userName, email);

  React.useEffect(() => {
    if (!menuOpen) {
      return;
    }

    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setMenuOpen(false);
      }
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [menuOpen]);

  if (!auth.clerkEnabled || !auth.isLoaded || !auth.isSignedIn) {
    return null;
  }

  const signOut = async () => {
    setMenuOpen(false);
    const clerk = auth.clerk as typeof auth.clerk & {
      signOut?: (options?: { redirectUrl?: string }) => Promise<void> | void;
    };

    await clerk?.signOut?.({ redirectUrl: "/giris" });
    window.location.href = "/giris";
  };

  return (
    <div className="systemcel-user" ref={rootRef}>
      <button
        className="systemcel-user-button"
        type="button"
        aria-label="Kullanıcı hesabı"
        aria-haspopup="menu"
        aria-expanded={menuOpen}
        onClick={() => setMenuOpen((current) => !current)}
      >
        <span className="systemcel-user-avatar">
          {auth.user?.imageUrl ? <img src={auth.user.imageUrl} alt="" /> : initials}
        </span>
        <span className="systemcel-user-button__text">
          <strong>{userName}</strong>
          {userHandle ? <small>{userHandle}</small> : null}
        </span>
        <ChevronUp className="systemcel-user-button__chevron" size={16} />
      </button>

      {menuOpen ? (
        <div className="systemcel-user-menu" role="menu">
          <div className="systemcel-user-menu__identity">
            <span className="systemcel-user-avatar">
              {auth.user?.imageUrl ? <img src={auth.user.imageUrl} alt="" /> : initials}
            </span>
            <div>
              <strong>{userName}</strong>
              {userHandle ? <small>{userHandle}</small> : null}
            </div>
          </div>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setMenuOpen(false);
              setProfileOpen(true);
            }}
          >
            <User size={17} />
            <span>Hesap bilgileri</span>
          </button>
          <button type="button" role="menuitem" onClick={signOut}>
            <LogOut size={17} />
            <span>Çıkış yap</span>
          </button>
        </div>
      ) : null}

      {profileOpen ? (
        <div className="systemcel-user-modal" role="dialog" aria-modal="true" aria-label="Hesap bilgileri">
          <section className="systemcel-user-modal__panel">
            <button className="systemcel-user-modal__close" type="button" aria-label="Kapat" onClick={() => setProfileOpen(false)}>
              <X size={20} />
            </button>
            <div className="systemcel-user-modal__avatar">
              <span className="systemcel-user-avatar">
                {auth.user?.imageUrl ? <img src={auth.user.imageUrl} alt="" /> : initials}
              </span>
            </div>
            <h2>Hesap bilgileri</h2>
            <div className="systemcel-user-modal__info">
              <span>
                <User size={17} />
                Ad Soyad
              </span>
              <strong>{userName}</strong>
            </div>
            {username ? (
              <div className="systemcel-user-modal__info">
                <span>
                  <User size={17} />
                  Kullanıcı adı
                </span>
                <strong>@{username}</strong>
              </div>
            ) : null}
            {email ? (
              <div className="systemcel-user-modal__info">
                <span>
                  <Mail size={17} />
                  E-posta
                </span>
                <strong>{email}</strong>
              </div>
            ) : null}
            <button className="systemcel-user-modal__signout" type="button" onClick={signOut}>
              <LogOut size={18} />
              Çıkış yap
            </button>
          </section>
        </div>
      ) : null}
    </div>
  );
}
