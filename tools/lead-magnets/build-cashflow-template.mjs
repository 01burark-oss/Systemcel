import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repoRoot = path.resolve(import.meta.dirname, "../..");
const outputDir = path.join(repoRoot, "outputs", "01a028e5-48dc-7f42-845b-925b8e8ad293");
const publicDir = path.join(repoRoot, "Systemcel.Web", "public", "kaynaklar", "dosyalar");
const qaDir = path.join(repoRoot, "qa", "lead-magnets", "cashflow");
const fileName = "systemcel-13-haftalik-nakit-akisi.xlsx";
await Promise.all([outputDir, publicDir, qaDir].map((dir) => fs.mkdir(dir, { recursive: true })));

const colors = {
  ink: "#10140D",
  lime: "#CBFF35",
  blue: "#1769AA",
  paleBlue: "#EEF6FF",
  paleLime: "#F5FFD8",
  line: "#D9E2EC",
  muted: "#617082",
  white: "#FFFFFF",
  input: "#FFF6CC",
  red: "#B42318",
  redPale: "#FFF0EE",
};

const workbook = Workbook.create();
const summary = workbook.worksheets.add("Özet");
const plan = workbook.worksheets.add("13 Haftalık Plan");
const items = workbook.worksheets.add("Plan Kalemleri");
const guide = workbook.worksheets.add("Kullanım");

for (const sheet of [summary, plan, items, guide]) {
  sheet.showGridLines = false;
}

// Plan Kalemleri — all editable source rows live here.
items.getRange("A1:G1").merge();
items.getRange("A1").values = [["13 haftalık nakit planı · kaynak kalemler"]];
items.getRange("A1:G1").format = {
  fill: colors.ink,
  font: { bold: true, color: colors.lime, size: 18 },
  rowHeight: 34,
  verticalAlignment: "center",
};
items.getRange("A2:G2").merge();
items.getRange("A2").values = [["Her tahsilat ve ödemeyi tek satırda yazın. Sarı alanlar sizin giriş alanlarınızdır."]];
items.getRange("A2:G2").format = { font: { color: colors.muted, italic: true }, rowHeight: 28 };
items.getRange("A5:G5").values = [["Tarih", "Tür", "Cari / kategori", "Açıklama", "Tutar", "Kesinlik", "Hafta"]];
items.getRange("A5:G5").format = {
  fill: colors.lime,
  font: { bold: true, color: colors.ink },
  horizontalAlignment: "center",
  rowHeight: 28,
  borders: { preset: "all", style: "thin", color: "#A9C82C" },
};
items.getRange("A6:F205").format = {
  fill: colors.input,
  borders: { preset: "all", style: "thin", color: colors.line },
  verticalAlignment: "center",
};
items.getRange("G6:G205").format = {
  fill: colors.paleBlue,
  borders: { preset: "all", style: "thin", color: colors.line },
  horizontalAlignment: "center",
};
const baseDateFormula = "DATE('13 Haftalık Plan'!$F$2,'13 Haftalık Plan'!$D$2,'13 Haftalık Plan'!$B$2)";
items.getRange("G6").formulas = [[`=IF(A6=\"\",\"\",IF(OR(A6<${baseDateFormula},A6>${baseDateFormula}+90),\"Dışarıda\",INT((A6-${baseDateFormula})/7)+1))`]];
items.getRange("G6:G205").fillDown();
items.getRange("A6:A205").setNumberFormat("dd.mm.yyyy");
items.getRange("E6:E205").setNumberFormat("₺#,##0.00;[Red]-₺#,##0.00");
items.getRange("B6:B205").dataValidation = { rule: { type: "list", values: ["Beklenen tahsilat", "Planlanan gelir", "Beklenen ödeme", "Planlanan gider"] } };
items.getRange("F6:F205").dataValidation = { rule: { type: "list", values: ["Kesin", "Beklenen", "Tahmini"] } };
items.getRange("A:A").format.columnWidth = 13;
items.getRange("B:B").format.columnWidth = 22;
items.getRange("C:C").format.columnWidth = 23;
items.getRange("D:D").format.columnWidth = 34;
items.getRange("E:E").format.columnWidth = 15;
items.getRange("F:F").format.columnWidth = 14;
items.getRange("G:G").format.columnWidth = 12;
items.freezePanes.freezeRows(5);

// 13-week calculated plan.
plan.getRange("A1:K1").merge();
plan.getRange("A1").values = [["13 haftalık nakit akışı"]];
plan.getRange("A1:K1").format = {
  fill: colors.ink,
  font: { bold: true, color: colors.lime, size: 20 },
  rowHeight: 36,
  verticalAlignment: "center",
};
plan.getRange("A2:F2").values = [["Gün", 31, "Ay", 8, "Yıl", 2026]];
plan.getRange("A3").values = [["Açılış kasa / banka"]];
plan.getRange("B3").values = [[0]];
plan.getRange("A2:A3").format = { font: { bold: true, color: colors.ink } };
plan.getRange("C2:E2").format = { font: { bold: true, color: colors.ink }, horizontalAlignment: "center" };
plan.getRange("B2").format = {
  fill: colors.input,
  font: { bold: true, color: colors.ink },
  borders: { preset: "all", style: "thin", color: "#D7BF55" },
};
plan.getRange("D2").format = {
  fill: colors.input,
  font: { bold: true, color: colors.ink },
  borders: { preset: "all", style: "thin", color: "#D7BF55" },
};
plan.getRange("F2").format = {
  fill: colors.input,
  font: { bold: true, color: colors.ink },
  borders: { preset: "all", style: "thin", color: "#D7BF55" },
};
plan.getRange("B3").format = {
  fill: colors.input,
  font: { bold: true, color: colors.ink },
  borders: { preset: "all", style: "thin", color: "#D7BF55" },
};
plan.getRange("B3").setNumberFormat("₺#,##0.00;[Red]-₺#,##0.00");
plan.getRange("H2:K3").merge();
plan.getRange("H2").values = [["1) Başlangıç gün/ay/yılını ve açılış bakiyesini yazın. 2) Plan Kalemleri sayfasını doldurun. 3) Bu sayfa kendiliğinden hesaplanır."]];
plan.getRange("H2:K3").format = { fill: colors.paleLime, font: { color: colors.ink }, wrapText: true, verticalAlignment: "center" };
plan.getRange("A6:K6").values = [["Hafta", "Başlangıç", "Bitiş", "Açılış", "Beklenen tahsilat", "Planlanan gelir", "Beklenen ödeme", "Planlanan gider", "Net akış", "Kapanış", "Not"]];
plan.getRange("A6:K6").format = {
  fill: colors.lime,
  font: { bold: true, color: colors.ink },
  horizontalAlignment: "center",
  wrapText: true,
  rowHeight: 38,
  borders: { preset: "all", style: "thin", color: "#A9C82C" },
};
const weekNumbers = Array.from({ length: 13 }, (_, index) => [index + 1]);
plan.getRange("A7:A19").values = weekNumbers;
const planBaseDate = "DATE($F$2,$D$2,$B$2)";
const visibleDate = (offset) => `=IF(DAY(${planBaseDate}+${offset})<10,\"0\",\"\")&DAY(${planBaseDate}+${offset})&\".\"&IF(MONTH(${planBaseDate}+${offset})<10,\"0\",\"\")&MONTH(${planBaseDate}+${offset})&\".\"&YEAR(${planBaseDate}+${offset})`;
plan.getRange("B7").formulas = [[visibleDate("7*(A7-1)")]];
plan.getRange("B7:B19").fillDown();
plan.getRange("C7").formulas = [[visibleDate("7*(A7-1)+6")]];
plan.getRange("C7:C19").fillDown();
plan.getRange("D7").formulas = [["=$B$3"]];
plan.getRange("D8").formulas = [["=J7"]];
plan.getRange("D8:D19").fillDown();
const sumifs = (kind) => `=SUMIFS('Plan Kalemleri'!$E$6:$E$205,'Plan Kalemleri'!$B$6:$B$205,\"${kind}\",'Plan Kalemleri'!$A$6:$A$205,\">=\"&(${planBaseDate}+7*(A7-1)),'Plan Kalemleri'!$A$6:$A$205,\"<=\"&(${planBaseDate}+7*(A7-1)+6))`;
plan.getRange("E7").formulas = [[sumifs("Beklenen tahsilat")]];
plan.getRange("F7").formulas = [[sumifs("Planlanan gelir")]];
plan.getRange("G7").formulas = [[sumifs("Beklenen ödeme")]];
plan.getRange("H7").formulas = [[sumifs("Planlanan gider")]];
plan.getRange("E7:H19").fillDown();
plan.getRange("I7").formulas = [["=SUM(E7:F7)-SUM(G7:H7)"]];
plan.getRange("I7:I19").fillDown();
plan.getRange("J7").formulas = [["=D7+I7"]];
plan.getRange("J7:J19").fillDown();
plan.getRange("A7:K19").format = { borders: { preset: "all", style: "thin", color: colors.line }, rowHeight: 25 };
plan.getRange("D7:J19").setNumberFormat("₺#,##0.00;[Red]-₺#,##0.00");
plan.getRange("E7:H19").format.fill = colors.paleBlue;
plan.getRange("J7:J19").format = { fill: colors.paleLime, font: { bold: true }, numberFormat: "₺#,##0.00;[Red]-₺#,##0.00" };
plan.getRange("J7:J19").conditionalFormats.add("cellIs", { operator: "lessThan", formula: 0, format: { fill: colors.redPale, font: { color: colors.red, bold: true } } });
plan.getRange("K7:K19").format.fill = colors.input;
plan.getRange("A:A").format.columnWidth = 8;
plan.getRange("B:C").format.columnWidth = 13;
plan.getRange("D:J").format.columnWidth = 17;
plan.getRange("K:K").format.columnWidth = 26;
plan.freezePanes.freezeRows(6);

// Executive summary with formula-backed chart.
summary.getRange("A1:J2").merge();
summary.getRange("A1").values = [["13 haftada paran nereye gidiyor?"]];
summary.getRange("A1:J2").format = {
  fill: colors.lime,
  font: { bold: true, color: colors.ink, size: 24 },
  verticalAlignment: "center",
};
summary.getRange("A3:J3").merge();
summary.getRange("A3").values = [["Plan Kalemleri sayfasına giriş yapın; haftalık görünüm ve risk işaretleri otomatik hesaplansın."]];
summary.getRange("A3:J3").format = { font: { color: colors.muted, italic: true }, rowHeight: 26 };
const cards = [
  ["A5:B5", "A6:B7", "Başlangıç", "=IF('13 Haftalık Plan'!$B$2<10,\"0\",\"\")&'13 Haftalık Plan'!$B$2&\".\"&IF('13 Haftalık Plan'!$D$2<10,\"0\",\"\")&'13 Haftalık Plan'!$D$2&\".\"&'13 Haftalık Plan'!$F$2", "@"],
  ["D5:E5", "D6:E7", "Açılış bakiyesi", "='13 Haftalık Plan'!$B$3", "₺#,##0.00"],
  ["G5:H5", "G6:H7", "En düşük kapanış", "=MIN('13 Haftalık Plan'!$J$7:$J$19)", "₺#,##0.00"],
  ["I5:J5", "I6:J7", "Negatif hafta", "=COUNTIF('13 Haftalık Plan'!$J$7:$J$19,\"<0\")", "0"],
];
for (const [labelRange, valueRange, label, formula, numberFormat] of cards) {
  summary.getRange(labelRange).merge();
  summary.getRange(valueRange).merge();
  summary.getRange(labelRange.split(":")[0]).values = [[label]];
  summary.getRange(valueRange.split(":")[0]).formulas = [[formula]];
  summary.getRange(labelRange).format = { fill: colors.ink, font: { bold: true, color: colors.white }, horizontalAlignment: "center" };
  summary.getRange(valueRange).format = { fill: colors.paleBlue, font: { bold: true, color: colors.blue, size: 17 }, horizontalAlignment: "center", verticalAlignment: "center" };
  summary.getRange(valueRange).setNumberFormat(numberFormat);
}
summary.getRange("A10:B10").values = [["Hafta", "Kapanış bakiyesi"]];
summary.getRange("A10:B10").format = { fill: colors.ink, font: { bold: true, color: colors.white } };
summary.getRange("A11").formulas = [["='13 Haftalık Plan'!A7"]];
summary.getRange("B11").formulas = [["='13 Haftalık Plan'!J7"]];
summary.getRange("A11:B23").fillDown();
summary.getRange("B11:B23").setNumberFormat("₺#,##0.00;[Red]-₺#,##0.00");
summary.getRange("A10:B23").format.borders = { preset: "all", style: "thin", color: colors.line };
const chart = summary.charts.add("line", summary.getRange("A10:B23"));
chart.title = "Haftalık kapanış bakiyesi";
chart.hasLegend = false;
chart.xAxis = { axisType: "textAxis" };
chart.yAxis = { numberFormatCode: "₺#,##0" };
chart.setPosition("D10", "J23");
summary.getRange("A25:J27").merge();
summary.getRange("A25").values = [["Kırmızı kapanış bakiyesi bir alarmdır: tarihi, tutarı veya planı güncelleyin. Şablon karar desteğidir; muhasebe veya yatırım danışmanlığı değildir."]];
summary.getRange("A25:J27").format = { fill: colors.paleLime, font: { color: colors.ink }, wrapText: true, verticalAlignment: "center" };
summary.getRange("A:J").format.columnWidth = 13;

// Usage and model boundaries.
guide.getRange("A1:F2").merge();
guide.getRange("A1").values = [["Bu şablonu 10 dakikada kur"]];
guide.getRange("A1:F2").format = { fill: colors.ink, font: { bold: true, color: colors.lime, size: 22 }, verticalAlignment: "center" };
guide.getRange("A4:F4").values = [["Adım", "Ne yapacaksınız?", null, null, null, "Kontrol"]];
guide.getRange("A4:F4").format = { fill: colors.lime, font: { bold: true, color: colors.ink }, rowHeight: 28 };
const steps = [
  ["1", "13 Haftalık Plan sayfasında başlangıç tarihini ve açılış kasa/banka bakiyesini değiştirin.", null, null, null, "Sarı iki hücre"],
  ["2", "Plan Kalemleri sayfasına beklenen her giriş ve çıkışı tek satır olarak yazın.", null, null, null, "Tarih + tür + tutar"],
  ["3", "Kesinlik alanını kullanın. Tahmini kalemleri haftalık görüşmede yeniden değerlendirin.", null, null, null, "Kesin / Beklenen / Tahmini"],
  ["4", "Özet sayfasındaki en düşük kapanış ve negatif hafta göstergelerini kontrol edin.", null, null, null, "Risk haftalarını açın"],
  ["5", "Her pazartesi gerçekleşenleri güncelleyin; eski tahminleri silmek yerine düzeltin.", null, null, null, "Haftalık ritim"],
];
guide.getRange("A5:F9").values = steps;
guide.getRange("A5:F9").format = { borders: { preset: "all", style: "thin", color: colors.line }, wrapText: true, verticalAlignment: "center", rowHeight: 48 };
guide.getRange("A12:F12").merge();
guide.getRange("A12").values = [["Modelin sınırları"]];
guide.getRange("A12:F12").format = { fill: colors.ink, font: { bold: true, color: colors.white } };
guide.getRange("A13:F16").merge(true);
guide.getRange("A13").values = [["• Yalnızca yazdığınız veriyi hesaplar; banka hesabınıza bağlanmaz.\n• Tutarları KDV dahil veya hariç yazabilirsiniz ama tüm dosyada aynı yaklaşımı kullanın.\n• Vadesi belirsiz kalemleri ‘Tahmini’ işaretleyin.\n• Son ödeme ve beyan tarihlerini mali müşavirinizle doğrulayın."]];
guide.getRange("A13:F16").format = { fill: colors.paleBlue, font: { color: colors.ink }, wrapText: true, verticalAlignment: "top" };
guide.getRange("A18:F18").merge();
guide.getRange("A18").values = [["Systemcel · systemcel.app · Sürüm: Eylül 2026"]];
guide.getRange("A18:F18").format = { font: { bold: true, color: colors.muted }, horizontalAlignment: "right" };
guide.getRange("A:A").format.columnWidth = 9;
guide.getRange("B:E").format.columnWidth = 21;
guide.getRange("F:F").format.columnWidth = 24;

const inspection = await workbook.inspect({ kind: "workbook,sheet,formula", maxChars: 8000, options: { maxResults: 120 } });
await fs.writeFile(path.join(qaDir, "inspection.json"), inspection.ndjson ?? JSON.stringify(inspection, null, 2), "utf8");

for (const sheetName of ["Özet", "13 Haftalık Plan", "Plan Kalemleri", "Kullanım"]) {
  const preview = await workbook.render({ sheetName, autoCrop: "all", scale: 1, format: "png" });
  const safeName = sheetName.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-zA-Z0-9]+/g, "-").toLowerCase();
  await fs.writeFile(path.join(qaDir, `${safeName}.png`), new Uint8Array(await preview.arrayBuffer()));
}

const output = await SpreadsheetFile.exportXlsx(workbook);
const canonicalPath = path.join(outputDir, fileName);
await output.save(canonicalPath);
await fs.copyFile(canonicalPath, path.join(publicDir, fileName));
console.log(canonicalPath);
