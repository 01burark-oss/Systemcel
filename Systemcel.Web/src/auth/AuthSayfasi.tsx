import React from "react";
import {
  ArrowRight,
  BarChart3,
  BriefcaseBusiness,
  Building2,
  ChevronDown,
  Clock3,
  Cloud,
  Globe2,
  Loader2,
  Mail,
  ShieldCheck,
  Sparkles,
  User,
  X
} from "lucide-react";
import systemcelIcon from "../assets/systemcel-icon.png";
import { legalTexts, type AuthLanguage, type LegalTextKey } from "./legalTexts";
import { useSystemcelAuth } from "./SystemcelAuthProvider";
import { AuthStatus } from "./AuthGate";

type AuthMode = "sign-in" | "sign-up";
type AuthStep = "form" | "verify-sign-in" | "verify-sign-up" | "forgot-request" | "reset-password";

type AuthCopy = {
  eyebrow: string;
  headlineStart: string;
  headlineAccent: string;
  headlineEnd: string;
  lead: string;
  cardTitle: string;
  cardText: string;
  switchText: string;
  switchCta: string;
  switchHref: string;
  legalPrefix: string;
  legalSuffix: string;
};

const LANGUAGE_STORAGE_KEY = "systemcel.auth.language";
const ACCOUNT_TYPE_INTENT_KEY = "systemcel.accountTypeIntent";

const languageOptions: Array<{ value: AuthLanguage; label: string; shortLabel: string }> = [
  { value: "tr", label: "Türkçe", shortLabel: "TR" },
  { value: "en", label: "English", shortLabel: "EN" }
];

const authCopy: Record<AuthLanguage, Record<AuthMode, AuthCopy>> = {
  tr: {
    "sign-in": {
      eyebrow: "AI destekli finans yönetimi",
      headlineStart: "Finansal süreçlerinizi daha",
      headlineAccent: "akıllı ve hızlı",
      headlineEnd: "yönetin",
      lead:
        "Gelişmiş analizler, otomatik raporlamalar ve gerçek zamanlı verilerle işletmenizin finansal gücünü artırın.",
      cardTitle: "Tekrar hoş geldiniz",
      cardText: "Hesabınıza giriş yaparak devam edin",
      switchText: "Hesabınız yok mu?",
      switchCta: "Hemen oluşturun",
      switchHref: "/kayit",
      legalPrefix: "Devam ederek",
      legalSuffix: "kabul etmiş olursunuz."
    },
    "sign-up": {
      eyebrow: "Güvenli web hesabı",
      headlineStart: "Finansal ekibinizi tek ve",
      headlineAccent: "hızlı",
      headlineEnd: "çalışma alanına taşıyın",
      lead:
        "Systemcel hesabınızı oluşturun, masaüstündeki verilerinizi güvenli aktarım akışıyla web çalışma alanına bağlayın.",
      cardTitle: "Hesabınızı oluşturun",
      cardText: "Finans yönetimine güvenli web hesabınızla başlayın",
      switchText: "Zaten hesabınız var mı?",
      switchCta: "Giriş yapın",
      switchHref: "/giris",
      legalPrefix: "Kayıt olarak",
      legalSuffix: "kabul etmiş olursunuz."
    }
  },
  en: {
    "sign-in": {
      eyebrow: "AI-assisted finance management",
      headlineStart: "Run your financial workflows",
      headlineAccent: "smarter and faster",
      headlineEnd: "",
      lead:
        "Improve your financial control with advanced analysis, automated reporting and real-time business data.",
      cardTitle: "Welcome back",
      cardText: "Sign in to continue to your account",
      switchText: "Do not have an account?",
      switchCta: "Create one",
      switchHref: "/kayit",
      legalPrefix: "By continuing, you agree to the",
      legalSuffix: "."
    },
    "sign-up": {
      eyebrow: "Secure web account",
      headlineStart: "Move your finance team into one",
      headlineAccent: "fast",
      headlineEnd: "workspace",
      lead:
        "Create your Systemcel account and connect your desktop data to the web workspace through a secure migration flow.",
      cardTitle: "Create your account",
      cardText: "Start finance management with your secure web account",
      switchText: "Already have an account?",
      switchCta: "Sign in",
      switchHref: "/giris",
      legalPrefix: "By signing up, you agree to the",
      legalSuffix: "."
    }
  }
};

const statusCopy = {
  tr: {
    missingKeyTitle: "Oturum altyapısı bekleniyor",
    missingKeyText: "Web hesabı ekranlarını açmak için Systemcel.Web/.env içinde oturum anahtarını tanımlayın.",
    loadingTitle: "Oturum hazırlanıyor",
    loadingText: "Giriş ekranı hazırlanıyor.",
    errorTitle: "Oturum servisi hazır değil",
    errorFallback: "Oturum istemcisi başlatılamadı.",
    backText: "Tanıtım sayfasına dön"
  },
  en: {
    missingKeyTitle: "Auth setup is pending",
    missingKeyText: "Define the auth key in Systemcel.Web/.env to enable web account screens.",
    loadingTitle: "Preparing sign-in",
    loadingText: "Preparing the sign-in screen.",
    errorTitle: "Auth service is not ready",
    errorFallback: "The auth client could not be initialized.",
    backText: "Back to intro"
  }
} satisfies Record<AuthLanguage, Record<string, string>>;

const proofItems = {
  tr: [
    { icon: ShieldCheck, title: "Verileriniz güvende", text: "256-bit şifreleme" },
    { icon: Cloud, title: "Bulut tabanlı erişim", text: "Her yerden erişin" },
    { icon: Clock3, title: "Gerçek zamanlı analiz", text: "Anlık içgörüler" }
  ],
  en: [
    { icon: ShieldCheck, title: "Data protected", text: "256-bit encryption" },
    { icon: Cloud, title: "Cloud access", text: "Work from anywhere" },
    { icon: Clock3, title: "Real-time analysis", text: "Live insights" }
  ]
} satisfies Record<AuthLanguage, Array<{ icon: typeof ShieldCheck; title: string; text: string }>>;

const visualCopy = {
  tr: {
    totalRevenue: "Toplam Gelir",
    cashFlow: "Nakit Akışı",
    aiAnalysis: "AI Analiz",
    aiInsight: "Optimum nakit akışı",
    months: ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas"]
  },
  en: {
    totalRevenue: "Total Revenue",
    cashFlow: "Cash Flow",
    aiAnalysis: "AI Analysis",
    aiInsight: "Optimum cash flow",
    months: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov"]
  }
} satisfies Record<AuthLanguage, { totalRevenue: string; cashFlow: string; aiAnalysis: string; aiInsight: string; months: string[] }>;

const formCopy = {
  tr: {
    "sign-in": {
      google: "Google ile devam et",
      divider: "veya e-posta ile giriş yap",
      email: "E-posta",
      password: "Parola",
      forgotLink: "Şifrenizi mi unuttunuz?",
      forgotTitle: "Şifre sıfırlama",
      forgotText: "Hesabınıza bağlı e-posta adresini yazın, size doğrulama kodu gönderelim.",
      forgotSubmit: "Kod gönder",
      forgotLoading: "Kod gönderiliyor",
      resetTitle: "Yeni şifre belirleyin",
      resetText: "E-postanıza gelen kodu ve yeni şifrenizi girin.",
      resetCode: "Doğrulama kodu",
      resetPassword: "Yeni parola",
      resetSubmit: "Şifreyi güncelle",
      resetLoading: "Şifre güncelleniyor",
      resetSent: "Şifre sıfırlama kodu e-posta adresinize gönderildi.",
      resetComplete: "Şifreniz güncellendi. Oturum açılıyor.",
      backToSignIn: "Girişe dön",
      submit: "Giriş yap",
      loading: "Giriş yapılıyor",
      codeTitle: "E-posta doğrulaması",
      codeText: "Güvenlik için e-postanıza gelen 6 haneli kodu girin.",
      codeLabel: "Doğrulama kodu",
      codeSubmit: "Doğrula"
    },
    "sign-up": {
      google: "Google ile kayıt ol",
      divider: "veya e-posta ile hesap oluştur",
      name: "Ad soyad",
      email: "E-posta",
      password: "Parola",
      submit: "Hesap oluştur",
      loading: "Hesap oluşturuluyor",
      codeTitle: "E-postanızı doğrulayın",
      codeText: "Hesabı tamamlamak için e-postanıza gelen 6 haneli kodu girin.",
      codeLabel: "Doğrulama kodu",
      codeSubmit: "Hesabı tamamla"
    }
  },
  en: {
    "sign-in": {
      google: "Continue with Google",
      divider: "or sign in with email",
      email: "Email",
      password: "Password",
      forgotLink: "Forgot your password?",
      forgotTitle: "Reset password",
      forgotText: "Enter the email address for your account and we'll send a verification code.",
      forgotSubmit: "Send code",
      forgotLoading: "Sending code",
      resetTitle: "Set a new password",
      resetText: "Enter the code from your email and your new password.",
      resetCode: "Verification code",
      resetPassword: "New password",
      resetSubmit: "Update password",
      resetLoading: "Updating password",
      resetSent: "A password reset code was sent to your email address.",
      resetComplete: "Your password was updated. Signing you in.",
      backToSignIn: "Back to sign in",
      submit: "Sign in",
      loading: "Signing in",
      codeTitle: "Email verification",
      codeText: "Enter the 6-digit code sent to your email.",
      codeLabel: "Verification code",
      codeSubmit: "Verify"
    },
    "sign-up": {
      google: "Sign up with Google",
      divider: "or create an account with email",
      name: "Full name",
      email: "Email",
      password: "Password",
      submit: "Create account",
      loading: "Creating account",
      codeTitle: "Verify your email",
      codeText: "Enter the 6-digit code sent to your email to finish setup.",
      codeLabel: "Verification code",
      codeSubmit: "Finish setup"
    }
  }
} satisfies Record<AuthLanguage, Record<AuthMode, Record<string, string>>>;

export function AuthSayfasi({ mode }: { mode: AuthMode }) {
  const auth = useSystemcelAuth();
  const returnUrl = getSafeReturnUrl();
  const accountTypeIntent = getAccountTypeIntent(mode);
  const [language, setLanguageState] = React.useState<AuthLanguage>(() => getInitialLanguage());
  const [languageMenuOpen, setLanguageMenuOpen] = React.useState(false);
  const [legalModal, setLegalModal] = React.useState<LegalTextKey | null>(null);
  const languageMenuRef = React.useRef<HTMLDivElement | null>(null);
  const copy = authCopy[language][mode];
  const status = statusCopy[language];
  const legal = legalTexts[language];
  const visual = visualCopy[language];

  React.useEffect(() => {
    document.documentElement.lang = language;
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
  }, [language]);

  React.useEffect(() => {
    if (accountTypeIntent) {
      window.localStorage.setItem(ACCOUNT_TYPE_INTENT_KEY, accountTypeIntent);
    }
  }, [accountTypeIntent]);

  React.useEffect(() => {
    if (!languageMenuOpen) return;

    const closeOnOutsideClick = (event: MouseEvent) => {
      if (!languageMenuRef.current?.contains(event.target as Node)) {
        setLanguageMenuOpen(false);
      }
    };

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setLanguageMenuOpen(false);
      }
    };

    document.addEventListener("mousedown", closeOnOutsideClick);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", closeOnOutsideClick);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [languageMenuOpen]);

  React.useEffect(() => {
    if (auth.clerkEnabled && auth.isLoaded && auth.isSignedIn) {
      window.location.replace(returnUrl);
    }
  }, [auth.clerkEnabled, auth.isLoaded, auth.isSignedIn, returnUrl]);

  const setLanguage = (nextLanguage: AuthLanguage) => {
    setLanguageState(nextLanguage);
    setLanguageMenuOpen(false);
  };

  if (!auth.clerkEnabled) {
    return (
      <AuthStatus
        title={status.missingKeyTitle}
        text={status.missingKeyText}
        actionHref="/"
        actionText={status.backText}
      />
    );
  }

  if (!auth.isLoaded) {
    return <AuthStatus title={status.loadingTitle} text={status.loadingText} />;
  }

  if (auth.error || !auth.clerk) {
    return (
      <AuthStatus
        title={status.errorTitle}
        text={auth.error || status.errorFallback}
        actionHref="/"
        actionText={status.backText}
      />
    );
  }

  return (
    <main className={`auth-shell auth-shell--branded auth-shell--${mode}`}>
      <a className="auth-shell__brand auth-shell__brand--top" href="/" aria-label="Systemcel">
        <span className="auth-shell__brand-mark">
          <img src={systemcelIcon} alt="" />
        </span>
        <span className="auth-shell__brand-copy">
          <strong>SYSTEMCEL</strong>
          <small>Finance Suite</small>
        </span>
      </a>

      <div className="auth-shell__language" ref={languageMenuRef}>
        <button
          className="auth-shell__lang"
          type="button"
          aria-label={language === "tr" ? "Dil seçimi" : "Language selection"}
          aria-expanded={languageMenuOpen}
          aria-haspopup="menu"
          onClick={() => setLanguageMenuOpen((current) => !current)}
        >
          <Globe2 size={19} />
          <span>{languageOptions.find((option) => option.value === language)?.shortLabel}</span>
          <ChevronDown size={16} />
        </button>
        {languageMenuOpen ? (
          <div className="auth-shell__lang-menu" role="menu">
            {languageOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                role="menuitemradio"
                aria-checked={language === option.value}
                className={language === option.value ? "active" : ""}
                onClick={() => setLanguage(option.value)}
              >
                <span>{option.shortLabel}</span>
                {option.label}
              </button>
            ))}
          </div>
        ) : null}
      </div>

      <section className="auth-shell__layout" aria-label="Systemcel giriş">
        <div className="auth-shell__left">
          <span className="auth-shell__eyebrow">
            <Sparkles size={18} />
            {copy.eyebrow}
          </span>
          <h1 className="auth-shell__headline">
            {copy.headlineStart} <span>{copy.headlineAccent}</span> {copy.headlineEnd}
          </h1>
          <p className="auth-shell__lead">{copy.lead}</p>

          <div className="auth-shell__proof" aria-label={language === "tr" ? "Systemcel avantajları" : "Systemcel benefits"}>
            {proofItems[language].map((item) => {
              const Icon = item.icon;
              return (
                <article key={item.title}>
                  <span>
                    <Icon size={28} />
                  </span>
                  <div>
                    <strong>{item.title}</strong>
                    <small>{item.text}</small>
                  </div>
                </article>
              );
            })}
          </div>

          <div className="auth-shell__dashboard" aria-hidden="true">
            <article className="auth-metric-card">
              <div>
                <span>{visual.totalRevenue}</span>
                <strong>₺ 32.950,00</strong>
              </div>
              <small>+76%</small>
              <div className="auth-metric-card__bars">
                {[26, 38, 21, 64, 30, 48, 42, 72].map((height, index) => (
                  <i key={index} style={{ height: `${height}%` }} />
                ))}
              </div>
            </article>

            <article className="auth-chart-card">
              <header>
                <span>{visual.cashFlow}</span>
                <BarChart3 size={18} />
              </header>
              <div className="auth-chart-card__plot">
                <div className="auth-chart-card__grid" />
                <svg viewBox="0 0 560 190" role="presentation" focusable="false">
                  <path
                    d="M8 148 C42 148 42 116 76 116 S116 133 145 102 197 106 224 86 265 44 310 57 329 112 368 94 432 88 452 47 504 60 548 26"
                    fill="none"
                    stroke="currentColor"
                    strokeLinecap="round"
                    strokeWidth="5"
                  />
                  <circle cx="310" cy="57" r="10" />
                  <path d="M310 68 L310 175" stroke="currentColor" strokeDasharray="7 7" strokeOpacity="0.42" />
                </svg>
              </div>
              <footer>
                {visual.months.map((month) => (
                  <span key={month}>{month}</span>
                ))}
              </footer>
            </article>

            <article className="auth-ai-chip">
              <Sparkles size={27} />
              <div>
                <strong>{visual.aiAnalysis}</strong>
                <span>{visual.aiInsight}</span>
              </div>
            </article>
          </div>
        </div>

        <section className="auth-shell__card" aria-label={copy.cardTitle}>
          <div className="auth-shell__card-head">
            <h2>{copy.cardTitle}</h2>
            <p>{copy.cardText}</p>
          </div>

          <SystemcelAuthForm mode={mode} returnUrl={returnUrl} language={language} accountTypeIntent={accountTypeIntent} />

          <div className="auth-shell__terms">
            {copy.legalPrefix}{" "}
            <button type="button" onClick={() => setLegalModal("terms")}>
              {legal.terms.linkLabel}
            </button>
            ,{" "}
            <button type="button" onClick={() => setLegalModal("privacy")}>
              {legal.privacy.linkLabel}
            </button>{" "}
            {language === "tr" ? "ve" : "and"}{" "}
            <button type="button" onClick={() => setLegalModal("kvkk")}>
              {legal.kvkk.linkLabel}
            </button>{" "}
            {copy.legalSuffix}
          </div>
          <a className="auth-shell__inline-link auth-shell__switch" href={buildAuthSwitchHref(copy.switchHref, accountTypeIntent, returnUrl)}>
            {copy.switchText}
            <span>{copy.switchCta}</span>
            <ArrowRight size={17} />
          </a>
        </section>
      </section>

      {legalModal ? (
        <LegalMetinPenceresi page={legalModal} language={language} onClose={() => setLegalModal(null)} />
      ) : null}
    </main>
  );
}

function SystemcelAuthForm({
  mode,
  returnUrl,
  language,
  accountTypeIntent
}: {
  mode: AuthMode;
  returnUrl: string;
  language: AuthLanguage;
  accountTypeIntent: "Isletme" | "Muhasebeci" | "";
}) {
  const auth = useSystemcelAuth();
  const copy: Record<string, string> = formCopy[language][mode];
  const signUpCopy: Record<string, string> = formCopy[language]["sign-up"];
  const [step, setStep] = React.useState<AuthStep>("form");
  const [fullName, setFullName] = React.useState("");
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [resetPassword, setResetPassword] = React.useState("");
  const [code, setCode] = React.useState("");
  const [accountType, setAccountType] = React.useState<"Isletme" | "Muhasebeci">(() => accountTypeIntent || "Isletme");
  const [islemde, setIslemde] = React.useState(false);
  const [hata, setHata] = React.useState("");
  const [bilgi, setBilgi] = React.useState("");

  const formId = React.useId();
  const codeMode = step === "verify-sign-in" || step === "verify-sign-up";
  const forgotRequestMode = step === "forgot-request";
  const resetPasswordMode = step === "reset-password";
  const selectedAccountType = mode === "sign-up" ? accountType : accountTypeIntent;
  const completionReturnUrl = returnUrlForAccountType(returnUrl, selectedAccountType);

  React.useEffect(() => {
    if (mode === "sign-up" && accountTypeIntent) {
      setAccountType(accountTypeIntent);
    }
  }, [accountTypeIntent, mode]);

  React.useEffect(() => {
    if (mode === "sign-up") {
      window.localStorage.setItem(ACCOUNT_TYPE_INTENT_KEY, accountType);
    }
  }, [accountType, mode]);

  const completeSession = React.useCallback(async (sessionId?: string | null) => {
    if (!sessionId) {
      throw new Error(language === "tr" ? "Oturum oluşturulamadı." : "Session could not be created.");
    }

    await auth.clerk?.setActive({ session: sessionId });
    window.location.replace(completionReturnUrl);
  }, [auth.clerk, completionReturnUrl, language]);

  const sifreSifirlamayaBasla = () => {
    setStep("forgot-request");
    setPassword("");
    setResetPassword("");
    setCode("");
    setHata("");
    setBilgi("");
  };

  const giriseDon = () => {
    setStep("form");
    setResetPassword("");
    setCode("");
    setHata("");
    setBilgi("");
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!auth.clerk || islemde) return;

    setIslemde(true);
    setHata("");
    setBilgi("");

    try {
      if (forgotRequestMode) {
        await auth.clerk.client.signIn.create({
          strategy: "reset_password_email_code",
          identifier: email.trim()
        });
        setPassword("");
        setResetPassword("");
        setCode("");
        setStep("reset-password");
        setBilgi(copy.resetSent || "");
        return;
      }

      if (resetPasswordMode) {
        const attempt = await auth.clerk.client.signIn.attemptFirstFactor({
          strategy: "reset_password_email_code",
          code: code.trim(),
          password: resetPassword
        });

        if (attempt.status === "complete") {
          setBilgi(copy.resetComplete || "");
          await completeSession(attempt.createdSessionId);
          return;
        }

        if (attempt.status === "needs_second_factor") {
          const emailFactor = attempt.supportedSecondFactors?.find((factor) => factor.strategy === "email_code");
          if (!emailFactor?.emailAddressId) {
            throw new Error(language === "tr" ? "Ek güvenlik doğrulaması gerekiyor." : "Additional verification is required.");
          }

          await auth.clerk.client.signIn.prepareSecondFactor({
            strategy: "email_code",
            emailAddressId: emailFactor.emailAddressId
          });
          setStep("verify-sign-in");
          setBilgi(language === "tr" ? "Şifreniz güncellendi. Ek doğrulama kodunu girin." : "Your password was updated. Enter the additional verification code.");
          return;
        }

        throw new Error(language === "tr" ? "Şifre sıfırlama tamamlanamadı." : "Password reset could not be completed.");
      }

      if (codeMode) {
        if (step === "verify-sign-up") {
          const attempt = await auth.clerk.client.signUp.attemptEmailAddressVerification({ code: code.trim() });
          if (attempt.status === "complete") {
            await completeSession(attempt.createdSessionId);
            return;
          }
        } else {
          const attempt = await auth.clerk.client.signIn.attemptSecondFactor({
            strategy: "email_code",
            code: code.trim()
          });
          if (attempt.status === "complete") {
            await completeSession(attempt.createdSessionId);
            return;
          }
        }

        throw new Error(language === "tr" ? "Doğrulama tamamlanamadı." : "Verification could not be completed.");
      }

      if (mode === "sign-in") {
        const attempt = await auth.clerk.client.signIn.create({
          identifier: email.trim(),
          password
        });

        if (attempt.status === "complete") {
          await completeSession(attempt.createdSessionId);
          return;
        }

        if (attempt.status === "needs_second_factor") {
          const emailFactor = attempt.supportedSecondFactors?.find((factor) => factor.strategy === "email_code");
          if (!emailFactor?.emailAddressId) {
            throw new Error(language === "tr" ? "Bu hesap için desteklenen doğrulama yöntemi bulunamadı." : "No supported verification method was found.");
          }

          await auth.clerk.client.signIn.prepareSecondFactor({
            strategy: "email_code",
            emailAddressId: emailFactor.emailAddressId
          });
          setStep("verify-sign-in");
          return;
        }

        throw new Error(language === "tr" ? "Giriş tamamlanamadı." : "Sign-in could not be completed.");
      }

      const { firstName, lastName } = splitName(fullName);
      const attempt = await auth.clerk.client.signUp.create({
        emailAddress: email.trim(),
        password,
        firstName,
        lastName
      });

      if (attempt.status === "complete") {
        await completeSession(attempt.createdSessionId);
        return;
      }

      await auth.clerk.client.signUp.prepareEmailAddressVerification({ strategy: "email_code" });
      setStep("verify-sign-up");
    } catch (error) {
      setHata(readableAuthError(error, language));
    } finally {
      setIslemde(false);
    }
  };

  const handleGoogle = async () => {
    if (!auth.clerk || islemde) return;

    setIslemde(true);
    setHata("");

    try {
      const resource = mode === "sign-up" ? auth.clerk.client.signUp : auth.clerk.client.signIn;
      const redirectPath = mode === "sign-up" ? "/kayit" : "/giris";
      const redirectUrl = new URL(redirectPath, window.location.origin);
      if (selectedAccountType)
        redirectUrl.searchParams.set("hesapTipi", selectedAccountType);
      redirectUrl.searchParams.set("returnUrl", completionReturnUrl);
      await resource.authenticateWithRedirect({
        strategy: "oauth_google",
        redirectUrl: redirectUrl.toString(),
        redirectUrlComplete: completionReturnUrl
      });
    } catch (error) {
      setHata(readableAuthError(error, language));
      setIslemde(false);
    }
  };

  return (
    <div className="auth-custom">
      {!forgotRequestMode && !resetPasswordMode ? (
        <>
          <button className="auth-custom__google" type="button" onClick={handleGoogle} disabled={islemde}>
            <span className="auth-custom__google-icon" aria-hidden="true">
              <GoogleLogo />
            </span>
            {copy.google}
          </button>

          <div className="auth-custom__divider">
            <span>{copy.divider}</span>
          </div>
        </>
      ) : null}

      <form className="auth-custom__form" onSubmit={handleSubmit}>
        {forgotRequestMode ? (
          <div className="auth-custom__verify">
            <strong>{copy.forgotTitle}</strong>
            <p>{copy.forgotText}</p>
            <label className="auth-custom__field" htmlFor={`${formId}-forgot-email`}>
              <span>
                <Mail size={16} />
                {copy.email}
              </span>
              <input
                id={`${formId}-forgot-email`}
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
                required
              />
            </label>
          </div>
        ) : resetPasswordMode ? (
          <div className="auth-custom__verify">
            <strong>{copy.resetTitle}</strong>
            <p>{copy.resetText}</p>
            <label className="auth-custom__field" htmlFor={`${formId}-reset-code`}>
              <span>{copy.resetCode}</span>
              <input
                id={`${formId}-reset-code`}
                value={code}
                onChange={(event) => setCode(event.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={8}
                required
              />
            </label>
            <label className="auth-custom__field" htmlFor={`${formId}-reset-password`}>
              <span>
                <ShieldCheck size={16} />
                {copy.resetPassword}
              </span>
              <input
                id={`${formId}-reset-password`}
                type="password"
                value={resetPassword}
                onChange={(event) => setResetPassword(event.target.value)}
                autoComplete="new-password"
                minLength={8}
                required
              />
            </label>
          </div>
        ) : codeMode ? (
          <div className="auth-custom__verify">
            <strong>{copy.codeTitle}</strong>
            <p>{copy.codeText}</p>
            <label className="auth-custom__field" htmlFor={`${formId}-code`}>
              <span>{copy.codeLabel}</span>
              <input
                id={`${formId}-code`}
                value={code}
                onChange={(event) => setCode(event.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={8}
                required
              />
            </label>
          </div>
        ) : (
          <>
            {mode === "sign-up" ? (
              <div className="auth-account-type" role="group" aria-label={language === "tr" ? "Hesap tipi" : "Account type"}>
                <button
                  type="button"
                  className={accountType === "Isletme" ? "active" : ""}
                  onClick={() => setAccountType("Isletme")}
                >
                  <Building2 size={17} />
                  <span>{language === "tr" ? "İşletme" : "Business"}</span>
                </button>
                <button
                  type="button"
                  className={accountType === "Muhasebeci" ? "active" : ""}
                  onClick={() => setAccountType("Muhasebeci")}
                >
                  <BriefcaseBusiness size={17} />
                  <span>{language === "tr" ? "Muhasebeci" : "Accountant"}</span>
                </button>
              </div>
            ) : null}

            {mode === "sign-up" ? (
              <label className="auth-custom__field" htmlFor={`${formId}-name`}>
                <span>
                  <User size={16} />
                  {signUpCopy.name}
                </span>
                <input
                  id={`${formId}-name`}
                  value={fullName}
                  onChange={(event) => setFullName(event.target.value)}
                  autoComplete="name"
                  required
                />
              </label>
            ) : null}

            <label className="auth-custom__field" htmlFor={`${formId}-email`}>
              <span>
                <Mail size={16} />
                {copy.email}
              </span>
              <input
                id={`${formId}-email`}
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
                required
              />
            </label>

            <label className="auth-custom__field" htmlFor={`${formId}-password`}>
              <span>
                <ShieldCheck size={16} />
                {copy.password}
              </span>
              <input
                id={`${formId}-password`}
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete={mode === "sign-up" ? "new-password" : "current-password"}
                minLength={8}
                required
              />
            </label>
            {mode === "sign-in" ? (
              <button className="auth-custom__link-button" type="button" onClick={sifreSifirlamayaBasla}>
                {copy.forgotLink}
              </button>
            ) : null}

            {mode === "sign-up" ? <div id="clerk-captcha" /> : null}
          </>
        )}

        {bilgi ? <p className="auth-custom__info">{bilgi}</p> : null}
        {hata ? <p className="auth-custom__error">{hata}</p> : null}

        <button className="auth-custom__submit" type="submit" disabled={islemde}>
          {islemde ? <Loader2 className="spin" size={18} /> : null}
          {islemde
            ? forgotRequestMode
              ? copy.forgotLoading
              : resetPasswordMode
                ? copy.resetLoading
                : copy.loading
            : forgotRequestMode
              ? copy.forgotSubmit
              : resetPasswordMode
                ? copy.resetSubmit
                : codeMode
                  ? copy.codeSubmit
                  : copy.submit}
        </button>
        {forgotRequestMode || resetPasswordMode ? (
          <button className="auth-custom__secondary" type="button" onClick={giriseDon} disabled={islemde}>
            {copy.backToSignIn}
          </button>
        ) : null}
      </form>
    </div>
  );
}

function GoogleLogo() {
  return (
    <svg viewBox="0 0 24 24" focusable="false" aria-hidden="true">
      <path
        fill="#4285F4"
        d="M23.49 12.27c0-.79-.07-1.54-.2-2.27H12v4.51h6.47a5.54 5.54 0 0 1-2.39 3.64v3.02h3.88c2.27-2.09 3.53-5.17 3.53-8.9Z"
      />
      <path
        fill="#34A853"
        d="M12 24c3.24 0 5.96-1.07 7.95-2.9l-3.88-3.02c-1.08.72-2.46 1.15-4.07 1.15-3.13 0-5.78-2.11-6.73-4.96H1.26v3.11A12 12 0 0 0 12 24Z"
      />
      <path
        fill="#FBBC05"
        d="M5.27 14.27a7.2 7.2 0 0 1 0-4.54V6.62H1.26a12 12 0 0 0 0 10.76l4.01-3.11Z"
      />
      <path
        fill="#EA4335"
        d="M12 4.77c1.76 0 3.35.61 4.6 1.8l3.44-3.44A11.56 11.56 0 0 0 12 0 12 12 0 0 0 1.26 6.62l4.01 3.11C6.22 6.88 8.87 4.77 12 4.77Z"
      />
    </svg>
  );
}

function LegalMetinPenceresi({
  page,
  language,
  onClose
}: {
  page: LegalTextKey;
  language: AuthLanguage;
  onClose: () => void;
}) {
  const content = legalTexts[language][page];

  React.useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  return (
    <div className="legal-modal" role="presentation" onMouseDown={onClose}>
      <section
        className="legal-modal__panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="legal-modal-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="legal-modal__header">
          <div>
            <h2 id="legal-modal-title">{content.title}</h2>
            <p>
              {content.updatedAtLabel}: {content.updatedAt}
            </p>
          </div>
          <button type="button" onClick={onClose} aria-label={content.closeLabel}>
            <X size={20} />
          </button>
        </header>
        <div className="legal-modal__body">
          <p className="legal-modal__intro">{content.intro}</p>
          {content.sections.map((section) => (
            <section key={section.title}>
              <h3>{section.title}</h3>
              <p>{section.text}</p>
            </section>
          ))}
          <p className="legal-modal__note">{content.note}</p>
        </div>
      </section>
    </div>
  );
}

function splitName(value: string) {
  const parts = value.trim().split(/\s+/).filter(Boolean);
  if (parts.length <= 1) {
    return { firstName: parts[0] ?? "", lastName: "" };
  }

  return {
    firstName: parts.slice(0, -1).join(" "),
    lastName: parts.at(-1) ?? ""
  };
}

function readableAuthError(error: unknown, language: AuthLanguage) {
  const fallback = language === "tr" ? "İşlem tamamlanamadı." : "The action could not be completed.";
  const raw = error as {
    message?: string;
    errors?: Array<{ code?: string; message?: string; longMessage?: string }>;
  };
  const first = raw.errors?.[0];
  const code = first?.code ?? "";

  const trMessages: Record<string, string> = {
    form_identifier_not_found: "Bu e-posta ile kayıtlı hesap bulunamadı.",
    form_password_incorrect: "Parola hatalı.",
    form_identifier_exists: "Bu e-posta ile zaten bir hesap var.",
    form_code_incorrect: "Doğrulama kodu hatalı.",
    form_password_pwned: "Bu parola güvenli görünmüyor. Daha güçlü bir parola seçin.",
    form_param_format_invalid: "Bilgileri kontrol edip tekrar deneyin."
  };

  if (language === "tr" && trMessages[code]) {
    return trMessages[code];
  }

  return first?.longMessage || first?.message || raw.message || fallback;
}

function getInitialLanguage(): AuthLanguage {
  const storedLanguage = window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
  return storedLanguage === "en" || storedLanguage === "tr" ? storedLanguage : "tr";
}

function getSafeReturnUrl() {
  const params = new URLSearchParams(window.location.search);
  const value = params.get("returnUrl");
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return "/app";
  }

  return value;
}

function getAccountTypeIntent(mode: AuthMode): "Isletme" | "Muhasebeci" | "" {
  const params = new URLSearchParams(window.location.search);
  const value = params.get("hesapTipi") || params.get("accountType") || (mode === "sign-up" ? window.localStorage.getItem(ACCOUNT_TYPE_INTENT_KEY) : "");
  if (value === "Muhasebeci" || value?.toLocaleLowerCase("tr-TR") === "muhasebeci") {
    return "Muhasebeci";
  }

  if (value === "Isletme" || value?.toLocaleLowerCase("tr-TR") === "isletme") {
    return "Isletme";
  }

  return "";
}

function returnUrlForAccountType(returnUrl: string, accountType: "Isletme" | "Muhasebeci" | "") {
  if (accountType === "Muhasebeci" && (returnUrl === "/app" || returnUrl === "/app/")) {
    return "/app/muhasebeci";
  }

  return returnUrl;
}

function buildAuthSwitchHref(baseHref: string, accountTypeIntent: "Isletme" | "Muhasebeci" | "", returnUrl: string) {
  const url = new URL(baseHref, window.location.origin);
  if (accountTypeIntent)
    url.searchParams.set("hesapTipi", accountTypeIntent);
  if (returnUrl !== "/app")
    url.searchParams.set("returnUrl", returnUrl);
  return `${url.pathname}${url.search}`;
}
