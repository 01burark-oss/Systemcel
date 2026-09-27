-- Read-only preview of structural marketplace reference inconsistencies.
-- Run only against an isolated database snapshot, or with a database role that
-- has SELECT-only access. Example: psql -v ON_ERROR_STOP=1 -f scripts/marketplace-reference-audit.sql
-- The transaction holds one repeatable-read snapshot and performs no writes.

BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY;

WITH issue_counts AS (
    SELECT
        'shipment_line_order_item_mismatch'::text AS issue_type,
        COUNT(*)::bigint AS issue_count
    FROM "TedarikciSevkiyatKalemi" AS shipment_line
    INNER JOIN "TedarikciSevkiyat" AS shipment
        ON shipment."Id" = shipment_line."TedarikciSevkiyatId"
    INNER JOIN "TedarikciSiparis" AS shipment_order
        ON shipment_order."Id" = shipment."TedarikciSiparisId"
    INNER JOIN "TedarikciSiparisKalemi" AS order_line
        ON order_line."Id" = shipment_line."TedarikciSiparisKalemiId"
    WHERE order_line."TedarikciSiparisId" <> shipment_order."Id"

    UNION ALL

    SELECT
        'receipt_label_order_mismatch'::text,
        COUNT(*)::bigint
    FROM "TedarikciMalKabul" AS receipt
    INNER JOIN "TedarikciSevkiyatEtiketi" AS label
        ON label."Id" = receipt."TedarikciSevkiyatEtiketiId"
    INNER JOIN "TedarikciSevkiyatKalemi" AS shipment_line
        ON shipment_line."Id" = label."TedarikciSevkiyatKalemiId"
    INNER JOIN "TedarikciSevkiyat" AS shipment
        ON shipment."Id" = shipment_line."TedarikciSevkiyatId"
    WHERE receipt."TedarikciSiparisId" <> shipment."TedarikciSiparisId"

    UNION ALL

    SELECT
        'payment_distribution_master_order_mismatch'::text,
        COUNT(*)::bigint
    FROM "PazaryeriOdemeDagitimi" AS distribution
    INNER JOIN "PazaryeriOdeme" AS payment
        ON payment."Id" = distribution."PazaryeriOdemeId"
    INNER JOIN "TedarikciSiparis" AS supplier_order
        ON supplier_order."Id" = distribution."TedarikciSiparisId"
    WHERE supplier_order."AnaSiparisId" <> payment."AnaSiparisId"

    UNION ALL

    SELECT
        'paid_settlement_missing_transfer_reference'::text,
        COUNT(*)::bigint
    FROM "TedarikciHakEdis" AS settlement
    WHERE settlement."OdenenTutar" > 0
      AND NULLIF(BTRIM(settlement."AktarimReferansi"), '') IS NULL

    UNION ALL

    SELECT
        'successful_payment_missing_provider_transaction_reference'::text,
        COUNT(*)::bigint
    FROM "PazaryeriOdeme" AS payment
    WHERE payment."Durum" IN ('Basarili', 'KismiIade', 'IadeEdildi')
      AND NULLIF(BTRIM(payment."SaglayiciIslemId"), '') IS NULL
)
SELECT issue_type, issue_count
FROM issue_counts
ORDER BY issue_type;

COMMIT;
