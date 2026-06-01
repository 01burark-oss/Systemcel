export function planAdiGoster(value?: string | null) {
  const normalized = (value ?? "").trim();
  if (!normalized)
    return "Ücretsiz";

  const key = normalized.toLocaleLowerCase("tr-TR");
  if (key === "ucretsiz")
    return "Ücretsiz";
  if (key === "baslangic")
    return "Başlangıç";
  if (key === "isletme")
    return "İşletme";

  return normalized;
}
