import React from "react";
import { Banknote, CreditCard, Landmark, WalletCards } from "lucide-react";
import type { FormDurumu, OdemeYontemi } from "./types";

export function simdiInputDegeri() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

export const bosForm = (): FormDurumu => ({
  id: null,
  tarih: simdiInputDegeri(),
  tur: "gelir",
  tutar: "",
  odemeYontemi: "nakit",
  kalem: "",
  aciklama: "",
  stokAktif: false,
  stokUrunId: 0,
  stokMiktar: "1"
});

export function paraBic(tutar: number) {
  return `${tutar.toLocaleString("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })} TL`;
}

export function tarihBic(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(date);
}

export function odemeEtiketi(value: OdemeYontemi) {
  switch (value) {
    case "krediKarti":
      return "Kredi Kartı";
    case "onlineOdeme":
      return "Online Ödeme";
    case "havale":
      return "Havale";
    default:
      return "Nakit";
  }
}

export function odemeIkonu(value: OdemeYontemi) {
  switch (value) {
    case "krediKarti":
      return <CreditCard size={17} />;
    case "onlineOdeme":
      return <WalletCards size={17} />;
    case "havale":
      return <Landmark size={17} />;
    default:
      return <Banknote size={17} />;
  }
}
