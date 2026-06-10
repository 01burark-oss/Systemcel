import { jsonOku } from "./json";

interface ProfilResmiYukleSonuc {
  url: string;
}

export async function muhasebeciProfilResmiYukle(file: File) {
  const formData = new FormData();
  formData.append("file", file);

  const sonuc = await jsonOku<ProfilResmiYukleSonuc>("/api/ekran/muhasebeci/profil-resmi", {
    method: "POST",
    body: formData
  });

  return sonuc.url;
}
