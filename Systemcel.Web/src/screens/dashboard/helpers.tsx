import React from "react";
import { Banknote, CreditCard, Landmark, WalletCards } from "lucide-react";

export function paraDegerBic(tutar: number) {
  return tutar.toLocaleString("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

export function paraBic(tutar: number) {
  return `${paraDegerBic(tutar)} TL`;
}

export function odemeIkonu(yontem: string) {
  switch (yontem) {
    case "Kredi Karti":
    case "Kredi Kartı":
      return <CreditCard size={18} />;
    case "Online Odeme":
    case "Online Ödeme":
      return <WalletCards size={18} />;
    case "Havale":
      return <Landmark size={18} />;
    default:
      return <Banknote size={18} />;
  }
}
