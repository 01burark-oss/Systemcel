import React from "react";

interface TelefonUlkesi {
  kod: string;
  ad: string;
  telefonKodu: string;
  ornek: string;
  desen: string;
  maksimumRakam: number;
  gruplar: number[];
  basSifirSil?: boolean;
}

const TELEFON_ULKELERI: TelefonUlkesi[] = [
  {
    kod: "TR",
    ad: "Türkiye",
    telefonKodu: "+90",
    ornek: "5xx xxx xxxx",
    desen: "5[0-9]{2}\\s[0-9]{3}\\s[0-9]{4}",
    maksimumRakam: 10,
    gruplar: [3, 3, 4],
    basSifirSil: true
  },
  {
    kod: "US",
    ad: "ABD",
    telefonKodu: "+1",
    ornek: "555 123 4567",
    desen: "[0-9]{3}\\s[0-9]{3}\\s[0-9]{4}",
    maksimumRakam: 10,
    gruplar: [3, 3, 4]
  },
  {
    kod: "GB",
    ad: "Birleşik Krallık",
    telefonKodu: "+44",
    ornek: "7400 123456",
    desen: "[0-9]{4}\\s[0-9]{6}",
    maksimumRakam: 10,
    gruplar: [4, 6],
    basSifirSil: true
  },
  {
    kod: "DE",
    ad: "Almanya",
    telefonKodu: "+49",
    ornek: "1512 3456789",
    desen: "[0-9]{3,4}\\s[0-9]{6,8}",
    maksimumRakam: 12,
    gruplar: [4, 8],
    basSifirSil: true
  }
];

interface TelefonNumarasiInputProps {
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  disabled?: boolean;
}

export function TelefonNumarasiInput({ value, onChange, required, disabled }: TelefonNumarasiInputProps) {
  const seciliUlke = ulkeBul(value);
  const ulkeRakamliKod = rakamlar(seciliUlke.telefonKodu);
  const ulusalRakamlar = ulusalRakam(value, seciliUlke, ulkeRakamliKod);
  const ulusalDeger = gruplandir(ulusalRakamlar, seciliUlke.gruplar);

  function ulkeDegisti(event: React.ChangeEvent<HTMLSelectElement>) {
    const yeniUlke = TELEFON_ULKELERI.find((item) => item.kod === event.target.value) ?? TELEFON_ULKELERI[0];
    const mevcutRakamlar = ulusalRakam(value, seciliUlke, ulkeRakamliKod).slice(0, yeniUlke.maksimumRakam);
    onChange(tamNumara(yeniUlke, mevcutRakamlar));
  }

  function numaraDegisti(event: React.ChangeEvent<HTMLInputElement>) {
    let digits = rakamlar(event.target.value);
    const pastedCountryCode = rakamlar(seciliUlke.telefonKodu);
    if (digits.startsWith(pastedCountryCode)) {
      digits = digits.slice(pastedCountryCode.length);
    }

    if (seciliUlke.basSifirSil) {
      digits = digits.replace(/^0+/, "");
    }

    onChange(tamNumara(seciliUlke, digits.slice(0, seciliUlke.maksimumRakam)));
  }

  return (
    <div className="phone-input">
      <select value={seciliUlke.kod} onChange={ulkeDegisti} disabled={disabled} aria-label="Ülke kodu">
        {TELEFON_ULKELERI.map((ulke) => (
          <option key={ulke.kod} value={ulke.kod}>
            {ulke.ad} {ulke.telefonKodu}
          </option>
        ))}
      </select>
      <input
        type="tel"
        value={ulusalDeger}
        onChange={numaraDegisti}
        placeholder={seciliUlke.ornek}
        pattern={seciliUlke.desen}
        title={`${seciliUlke.ad}: ${seciliUlke.telefonKodu} ${seciliUlke.ornek}`}
        autoComplete="tel-national"
        required={required}
        disabled={disabled}
      />
    </div>
  );
}

function ulkeBul(value: string) {
  const trimmed = value.trim();
  return TELEFON_ULKELERI.find((ulke) => trimmed.startsWith(ulke.telefonKodu)) ?? TELEFON_ULKELERI[0];
}

function ulusalRakam(value: string, ulke: TelefonUlkesi, ulkeRakamliKod: string) {
  let digits = rakamlar(value);
  if (digits.startsWith(ulkeRakamliKod)) {
    digits = digits.slice(ulkeRakamliKod.length);
  }

  if (ulke.basSifirSil) {
    digits = digits.replace(/^0+/, "");
  }

  return digits.slice(0, ulke.maksimumRakam);
}

function tamNumara(ulke: TelefonUlkesi, ulusalRakamlar: string) {
  const formatted = gruplandir(ulusalRakamlar, ulke.gruplar);
  return formatted ? `${ulke.telefonKodu} ${formatted}` : "";
}

function gruplandir(value: string, groups: number[]) {
  const parts: string[] = [];
  let start = 0;
  for (const group of groups) {
    const part = value.slice(start, start + group);
    if (!part)
      break;

    parts.push(part);
    start += group;
  }

  if (start < value.length) {
    parts.push(value.slice(start));
  }

  return parts.join(" ");
}

function rakamlar(value: string) {
  return value.replace(/\D/g, "");
}
