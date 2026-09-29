-- Session-local shadow tables test each audit predicate without changing
-- migrated tables or needing to violate production foreign keys.
CREATE TEMP TABLE "TedarikciSevkiyatKalemi" (
    "Id" integer, "TedarikciSevkiyatId" integer, "TedarikciSiparisKalemiId" integer
);
CREATE TEMP TABLE "TedarikciSevkiyat" (
    "Id" integer, "TedarikciSiparisId" integer
);
CREATE TEMP TABLE "TedarikciSiparis" (
    "Id" integer, "AnaSiparisId" integer
);
CREATE TEMP TABLE "TedarikciSiparisKalemi" (
    "Id" integer, "TedarikciSiparisId" integer
);
CREATE TEMP TABLE "TedarikciMalKabul" (
    "TedarikciSiparisId" integer, "TedarikciSevkiyatEtiketiId" integer
);
CREATE TEMP TABLE "TedarikciSevkiyatEtiketi" (
    "Id" integer, "TedarikciSevkiyatKalemiId" integer
);
CREATE TEMP TABLE "PazaryeriOdemeDagitimi" (
    "PazaryeriOdemeId" integer, "TedarikciSiparisId" integer
);
CREATE TEMP TABLE "PazaryeriOdeme" (
    "Id" integer, "AnaSiparisId" integer, "Durum" text, "SaglayiciIslemId" text
);
CREATE TEMP TABLE "TedarikciHakEdis" (
    "OdenenTutar" numeric, "AktarimReferansi" text
);

INSERT INTO "TedarikciSiparis" VALUES (40, 60), (41, 61);
INSERT INTO "TedarikciSiparisKalemi" VALUES (30, 41);
INSERT INTO "TedarikciSevkiyat" VALUES (20, 40);
INSERT INTO "TedarikciSevkiyatKalemi" VALUES (10, 20, 30);
INSERT INTO "TedarikciSevkiyatEtiketi" VALUES (50, 10);
INSERT INTO "TedarikciMalKabul" VALUES (41, 50);
INSERT INTO "PazaryeriOdeme" VALUES (70, 61, 'Basarili', '');
INSERT INTO "PazaryeriOdemeDagitimi" VALUES (70, 40);
INSERT INTO "TedarikciHakEdis" VALUES (10, '');
