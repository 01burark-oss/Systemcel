from __future__ import annotations

import os
import shutil
from pathlib import Path

from reportlab.graphics.barcode import qr
from reportlab.graphics.shapes import Drawing
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = REPO_ROOT / "output" / "pdf"
PUBLIC_DIR = REPO_ROOT / "Systemcel.Web" / "public" / "kaynaklar" / "dosyalar"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
PUBLIC_DIR.mkdir(parents=True, exist_ok=True)

INK = colors.HexColor("#10140D")
LIME = colors.HexColor("#CBFF35")
BLUE = colors.HexColor("#1769AA")
PALE_BLUE = colors.HexColor("#EEF6FF")
PALE_LIME = colors.HexColor("#F5FFD8")
MUTED = colors.HexColor("#5F6E7E")
LINE = colors.HexColor("#D9E2EC")
ORANGE = colors.HexColor("#F4A261")
PALE_ORANGE = colors.HexColor("#FFF4E8")
WHITE = colors.white

pdfmetrics.registerFont(TTFont("Arial", r"C:\Windows\Fonts\arial.ttf"))
pdfmetrics.registerFont(TTFont("Arial-Bold", r"C:\Windows\Fonts\arialbd.ttf"))
pdfmetrics.registerFont(TTFont("Arial-Italic", r"C:\Windows\Fonts\ariali.ttf"))
pdfmetrics.registerFont(TTFont("Georgia", r"C:\Windows\Fonts\georgia.ttf"))
pdfmetrics.registerFont(TTFont("Georgia-Bold", r"C:\Windows\Fonts\georgiab.ttf"))

PAGE_W, PAGE_H = A4
styles = getSampleStyleSheet()
BODY = ParagraphStyle(
    "BodyTR",
    parent=styles["BodyText"],
    fontName="Arial",
    fontSize=9.4,
    leading=13.8,
    textColor=INK,
    spaceAfter=5,
)
SMALL = ParagraphStyle("SmallTR", parent=BODY, fontSize=7.6, leading=10.4, textColor=MUTED)
TITLE = ParagraphStyle(
    "TitleTR",
    parent=styles["Title"],
    fontName="Georgia-Bold",
    fontSize=29,
    leading=33,
    textColor=INK,
    alignment=TA_LEFT,
    spaceAfter=9,
)
SUBTITLE = ParagraphStyle(
    "SubtitleTR",
    parent=BODY,
    fontSize=12.5,
    leading=17,
    textColor=MUTED,
)
H1 = ParagraphStyle(
    "H1TR",
    parent=styles["Heading1"],
    fontName="Georgia-Bold",
    fontSize=19,
    leading=23,
    textColor=INK,
    spaceBefore=5,
    spaceAfter=10,
)
H2 = ParagraphStyle(
    "H2TR",
    parent=styles["Heading2"],
    fontName="Arial-Bold",
    fontSize=12.5,
    leading=16,
    textColor=INK,
    spaceBefore=5,
    spaceAfter=6,
)
WHITE_SMALL = ParagraphStyle("WhiteSmall", parent=SMALL, textColor=WHITE)
LINK = ParagraphStyle("LinkTR", parent=BODY, textColor=BLUE, fontName="Arial-Bold")
CHECK = ParagraphStyle("CheckTR", parent=BODY, leftIndent=0, firstLineIndent=0, bulletIndent=0, spaceAfter=3)


def footer(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(LINE)
    canvas.line(18 * mm, 13 * mm, PAGE_W - 18 * mm, 13 * mm)
    canvas.setFont("Arial-Bold", 7.5)
    canvas.setFillColor(INK)
    canvas.drawString(18 * mm, 8.2 * mm, "SYSTEMCEL")
    canvas.setFont("Arial", 7.5)
    canvas.setFillColor(MUTED)
    canvas.drawString(39 * mm, 8.2 * mm, "systemcel.app")
    canvas.drawRightString(PAGE_W - 18 * mm, 8.2 * mm, f"{doc.page}")
    canvas.restoreState()


def make_doc(path: Path, title: str) -> SimpleDocTemplate:
    return SimpleDocTemplate(
        str(path),
        pagesize=A4,
        rightMargin=18 * mm,
        leftMargin=18 * mm,
        topMargin=16 * mm,
        bottomMargin=18 * mm,
        title=title,
        author="Systemcel",
        creator="Systemcel",
    )


def brand_band(kicker: str):
    band = Table(
        [[Paragraph(f"<b>■ SYSTEMCEL</b> &nbsp;&nbsp; {kicker.upper()}", BODY)]],
        colWidths=[PAGE_W - 36 * mm],
    )
    band.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), LIME),
                ("TEXTCOLOR", (0, 0), (-1, -1), INK),
                ("LEFTPADDING", (0, 0), (-1, -1), 10),
                ("RIGHTPADDING", (0, 0), (-1, -1), 10),
                ("TOPPADDING", (0, 0), (-1, -1), 8),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 8),
            ]
        )
    )
    return band


def cover(kicker: str, title: str, subtitle: str, note: str):
    note_table = Table([[Paragraph(note, BODY)]], colWidths=[PAGE_W - 36 * mm])
    note_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), PALE_LIME),
                ("BOX", (0, 0), (-1, -1), 0.7, LIME),
                ("LEFTPADDING", (0, 0), (-1, -1), 12),
                ("RIGHTPADDING", (0, 0), (-1, -1), 12),
                ("TOPPADDING", (0, 0), (-1, -1), 11),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 11),
            ]
        )
    )
    return [
        brand_band(kicker),
        Spacer(1, 22 * mm),
        Paragraph(title, TITLE),
        Paragraph(subtitle, SUBTITLE),
        Spacer(1, 12 * mm),
        note_table,
        Spacer(1, 12 * mm),
        Paragraph("Eylül 2026 · Ücretsiz çalışma kaynağı", SMALL),
        PageBreak(),
    ]


def section_label(text: str):
    table = Table([[Paragraph(text.upper(), WHITE_SMALL)]], colWidths=[PAGE_W - 36 * mm])
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), INK),
                ("LEFTPADDING", (0, 0), (-1, -1), 8),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]
        )
    )
    return table


def callout(text: str, color=PALE_BLUE, border=BLUE):
    table = Table([[Paragraph(text, BODY)]], colWidths=[PAGE_W - 36 * mm])
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), color),
                ("LINEBEFORE", (0, 0), (0, -1), 4, border),
                ("LEFTPADDING", (0, 0), (-1, -1), 11),
                ("RIGHTPADDING", (0, 0), (-1, -1), 10),
                ("TOPPADDING", (0, 0), (-1, -1), 9),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 9),
            ]
        )
    )
    return table


def question_rows(start: int, questions: list[str]):
    rows = []
    for offset, text in enumerate(questions):
        number = start + offset
        rows.append(
            Table(
                [[Paragraph(f"<b>{number:02}</b>", BODY), Paragraph(text, BODY)]],
                colWidths=[15 * mm, PAGE_W - 55 * mm],
                style=TableStyle(
                    [
                        ("BACKGROUND", (0, 0), (0, 0), LIME),
                        ("VALIGN", (0, 0), (-1, -1), "TOP"),
                        ("ALIGN", (0, 0), (0, 0), "CENTER"),
                        ("BOX", (0, 0), (-1, -1), 0.5, LINE),
                        ("LEFTPADDING", (0, 0), (0, -1), 3),
                        ("RIGHTPADDING", (0, 0), (0, -1), 3),
                        ("LEFTPADDING", (1, 0), (1, -1), 7),
                        ("RIGHTPADDING", (1, 0), (1, -1), 7),
                        ("TOPPADDING", (0, 0), (-1, -1), 7),
                        ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
                    ]
                ),
            )
        )
        rows.append(Spacer(1, 3 * mm))
    return rows


def build_ai():
    name = "systemcel-defterine-sorulacak-20-soru.pdf"
    path = OUTPUT_DIR / name
    story = cover(
        "AI",
        "Defterine sorulacak<br/>20 doğru soru",
        "Genel tavsiye istemek yerine kayıtlı veriyi hedefleyen sorular sorun. Daha kısa, denetlenebilir ve işe yarar yanıt alın.",
        "Altın kural: AI yalnızca sisteme girilmiş kayıtları görebilir. Eksik maaş, tahsilat veya ödeme planı varsa yanıt da eksik kalır.",
    )
    groups = [
        (
            "1 · Kasa ve dönem özeti",
            [
                "Bugünkü kayıtlı kasa ve banka bakiyemin özeti nedir?",
                "Bu ay gelir ile gider arasındaki fark ne kadar?",
                "Son 30 günde en yüksek üç gider kategorisi hangileri?",
                "Bu ayın kayıtlı gelirini geçen ayla karşılaştır; belirgin farkları yaz.",
                "Önümüzdeki 13 haftada kapanış bakiyesi en düşük olan hafta hangisi?",
            ],
        ),
        (
            "2 · Tahsilat ve cari görünümü",
            [
                "Toplam açık alacağım ve vadesi geçmiş kısmı ne kadar?",
                "Vadesi en çok geçmiş açık faturaları tarih ve tutarla sırala.",
                "Hangi müşterilerin ödeme ritmi son dönemde kötüleşiyor?",
                "Tahsilatı sürekli geciken müşterileri, tamamlanan ödeme geçmişine göre göster.",
                "Tek bir müşteriye yoğunlaşan alacak riskim var mı? Oranı ve dayanağını yaz.",
            ],
        ),
        (
            "3 · Plan ve nakit yeterliliği",
            [
                "13 haftalık planda negatif kapanış görünen haftaları ve açığın tutarını listele.",
                "Planlanan maaş ödemesi kayıtlıysa, o haftadaki kapanış bakiyesi yeterli mi?",
                "Önümüzdeki dört haftanın beklenen tahsilat ve ödeme toplamlarını karşılaştır.",
                "En büyük planlanan gider hangi haftada ve kapanış bakiyesine etkisi ne?",
                "Kayıtlı plan kalemlerinden hangileri ‘tahmini’ ve güncellenmeye ihtiyaç duyuyor?",
            ],
        ),
        (
            "4 · Daha güvenli karar soruları",
            [
                "[Müşteri adı] için açık alacak, gecikme ve ödeme ritmini özetle; nihai karar verme.",
                "Bu müşteriye yeni satış öncesi kontrol etmem gereken kayıtlı risk göstergeleri neler?",
                "Stokta kritik seviyenin altına düşen ürünleri ve kayıtlı uyarıları göster.",
                "Yanıtın hangi kayıtlara dayanıyor; eksik veya güncel olmayan veriyi ayrıca belirt.",
                "Bu özeti mali müşavire göndereceğim: doğrulanması gereken maddeleri ayrı listele.",
            ],
        ),
    ]
    number = 1
    for index, (label, questions) in enumerate(groups):
        story += [section_label(label), Spacer(1, 5 * mm)]
        story += question_rows(number, questions)
        number += len(questions)
        if index == 1:
            story += [PageBreak()]
    story += [
        Spacer(1, 4 * mm),
        callout(
            "<b>Üç güvenlik cümlesi:</b> “Bana karar verme, veriyi özetle.” · “Eksik veriyi söyle.” · “Tarih ve tutar dayanağını göster.”",
            PALE_LIME,
            LIME,
        ),
        Spacer(1, 5 * mm),
        Paragraph("Bu liste finansal, hukuki veya vergisel danışmanlık değildir; kayıt kontrolü ve soru tasarımı içindir.", SMALL),
    ]
    make_doc(path, "Defterine Sorulacak 20 Soru").build(story, onFirstPage=footer, onLaterPages=footer)
    shutil.copy2(path, PUBLIC_DIR / name)
    return path


def checklist_section(title: str, rows: list[str]):
    data = [[Paragraph("□", H2), Paragraph(item, BODY)] for item in rows]
    table = Table(data, colWidths=[10 * mm, PAGE_W - 50 * mm])
    table.setStyle(
        TableStyle(
            [
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("GRID", (0, 0), (-1, -1), 0.45, LINE),
                ("BACKGROUND", (0, 0), (0, -1), PALE_LIME),
                ("ALIGN", (0, 0), (0, -1), "CENTER"),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]
        )
    )
    return KeepTogether([section_label(title), Spacer(1, 3 * mm), table, Spacer(1, 5 * mm)])


def build_defter():
    name = "systemcel-ay-sonu-kapanis-kontrol-listesi.pdf"
    path = OUTPUT_DIR / name
    story = cover(
        "DEFTER",
        "Ay sonu kapanış<br/>kontrol listesi",
        "Dağınık kayıtları mali müşavire göndermeden önce tek turda tamamlayın. Her kutu ya işaretlensin ya da yanına sorumlu ve tarih yazılsın.",
        "Bu kontrol listesi ürün özelliği anlatmaz; işletme içi kapanış disiplinidir. Vergisel kapsamınızı ve süreleri mali müşavirinizle doğrulayın.",
    )
    sections = [
        ("1 · Satış ve alış belgeleri", [
            "Ay içindeki tüm satış faturalarının taslak/kesilmiş/iptal durumlarını kontrol ettim.",
            "Eksik alış faturalarını ve gider belgelerini satıcıdan istedim.",
            "İade, iskonto ve iptal belgelerini ilgili asıl işlemle eşleştirdim.",
            "e-Fatura/e-Arşiv ve diğer kanallardaki belgelerin dönemini doğruladım.",
        ]),
        ("2 · Tahsilat, ödeme ve cariler", [
            "Banka, nakit ve sanal POS tahsilatlarını ilgili cari/faturayla eşleştirdim.",
            "Kısmi ödemeleri ve mahsupları doğru tutar ve tarihle kaydettim.",
            "Vadesi geçmiş alacakları cari bazında gözden geçirdim.",
            "Mutabakat gerektiren cari farklarına sorumlu ve dönüş tarihi atadım.",
        ]),
        ("3 · Kasa, banka ve kartlar", [
            "Kasa sayımıyla kayıtlı kasa bakiyesini karşılaştırdım.",
            "Banka ekstrelerinin ay sonu bakiyesini kayıtlarla karşılaştırdım.",
            "Kredi kartı/POS komisyonu ve bloke çözülme farklarını ayırdım.",
            "Sahibi belirsiz para giriş ve çıkışlarını açıklığa kavuşturdum.",
        ]),
        ("4 · Stok ve operasyon", [
            "Fiziki stok ile kayıt farkı olan kritik ürünleri listeledim.",
            "Fire, sayım farkı, iade ve kişisel kullanım hareketlerini belgeledim.",
            "Negatif stokları ve olağandışı maliyet değişimlerini inceledim.",
            "Bekleyen sevk/teslim belgelerinin fatura durumunu kontrol ettim.",
        ]),
        ("5 · Dönem sonu ve devir", [
            "Personel/maaş, vergi, kira ve düzenli giderlerin kayıt durumunu kontrol ettim.",
            "Sonraki 13 haftanın büyük tahsilat ve ödeme kalemlerini güncelledim.",
            "Mali müşavire iletilecek belgeleri tek klasörde ve anlaşılır adlarla hazırladım.",
            "Açık soruları; konu, tutar, belge ve sorumlu bilgisiyle ayrı listeledim.",
            "Kapanış tarihini, hazırlayanı ve son kontrol edeni not ettim.",
        ]),
    ]
    for index, (title, rows) in enumerate(sections):
        story.append(checklist_section(title, rows))
        if index == 2:
            story.append(PageBreak())
    story += [
        Spacer(1, 3 * mm),
        section_label("Kapanış imzası"),
        Spacer(1, 3 * mm),
        Table(
            [["Dönem", "Hazırlayan", "Son kontrol", "Tarih"], ["", "", "", ""]],
            colWidths=[40 * mm, 50 * mm, 50 * mm, 35 * mm],
            rowHeights=[9 * mm, 15 * mm],
            style=TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, 0), LIME),
                    ("FONTNAME", (0, 0), (-1, 0), "Arial-Bold"),
                    ("FONTNAME", (0, 1), (-1, 1), "Arial"),
                    ("GRID", (0, 0), (-1, -1), 0.6, LINE),
                    ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                    ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ]
            ),
        ),
        Spacer(1, 5 * mm),
        callout("Kapanış, her şeyin kusursuz olması değil; açık kalan her maddenin görünür, sahipli ve tarihli olmasıdır.", PALE_LIME, LIME),
    ]
    make_doc(path, "Ay Sonu Kapanış Kontrol Listesi").build(story, onFirstPage=footer, onLaterPages=footer)
    shutil.copy2(path, PUBLIC_DIR / name)
    return path


def build_calendar():
    name = "systemcel-eylul-2026-beyanname-takvimi.pdf"
    path = OUTPUT_DIR / name
    story = cover(
        "TAKVİM",
        "Eylül 2026<br/>beyanname takvimi",
        "GİB’in 2026 yıllık vergi takviminden işletmeler için pratik bir kontrol görünümü. Her tarih her mükellefe uygulanmaz.",
        "Önce mükellefiyet türünüzü ve özel durumunuzu mali müşavirinizle eşleştirin. GİB erteleme ve güncellemeleri için canlı takvimi ayrıca kontrol edin.",
    )
    story += [
        section_label("Genel işletmeler için öne çıkan tarihler"),
        Spacer(1, 4 * mm),
    ]
    key_rows = [
        ["10 Eyl", "e-Defter berat", "Gelir vergisi mükellefleri için; yükleme tercihine göre Nisan–Haziran veya Mayıs dönemleri."],
        ["14 Eyl", "e-Defter berat", "Diğer mükellefler için; yükleme tercihine göre Nisan–Haziran veya Mayıs dönemleri."],
        ["25 Eyl", "Tevkif KDV", "Ağustos 2026 dönemine ait vergi sorumlularının tevkif ettikleri KDV’nin beyan ve ödemesi."],
        ["28 Eyl", "KDV", "Ağustos 2026 dönemine ait Katma Değer Vergisinin beyan ve ödemesi."],
        ["28 Eyl", "Muhtasar + prim", "Ağustos 2026 dönemi tevkifatlarının Muhtasar ve Prim Hizmet Beyannamesi ile beyan ve ödemesi."],
        ["28 Eyl", "Damga vergisi", "Ağustos 2026 dönemi ilgili damga vergilerinin beyan ve ödemesi."],
    ]
    table_data = [[Paragraph("Son gün", BODY), Paragraph("Başlık", BODY), Paragraph("Kontrol", BODY)]] + [
        [Paragraph(a, BODY), Paragraph(b, BODY), Paragraph(c, BODY)] for a, b, c in key_rows
    ]
    key_table = Table(table_data, colWidths=[25 * mm, 37 * mm, 113 * mm], repeatRows=1)
    key_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), LIME),
                ("FONTNAME", (0, 0), (-1, 0), "Arial-Bold"),
                ("GRID", (0, 0), (-1, -1), 0.5, LINE),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("TOPPADDING", (0, 0), (-1, -1), 7),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
            ]
        )
    )
    story += [key_table, Spacer(1, 6 * mm)]
    story += [
        callout(
            "<b>Ba/Bs notu:</b> Form Ba ve Form Bs bildirimleri Eylül 2024 döneminden itibaren kaldırıldığı için bu takvimde yer almıyor.",
            PALE_LIME,
            LIME,
        ),
        PageBreak(),
        section_label("Sektöre veya özel duruma bağlı diğer tarihler"),
        Spacer(1, 4 * mm),
    ]
    other_rows = [
        ["7 Eyl", "Noterlerce yapılan makbuz karşılığı ödemeler (Ağustos dönemi)."],
        ["10 Eyl", "16–31 Ağustos petrol/doğalgaz ÖTV; ilgili e-Defter beratları."],
        ["15 Eyl", "BSMV, KKDF, ticaret sicili harçları; belirli ÖTV ve özel iletişim vergileri."],
        ["21 Eyl", "Şans oyunları, ilan/reklam, eğlence, elektrik-havagazı ve yangın sigortası vergileri."],
        ["25 Eyl", "1–15 Eylül petrol/doğalgaz ÖTV."],
        ["28 Eyl", "Konaklama vergisi; ilgili KDV, muhtasar/prim ve damga vergisi yükümlülükleri."],
        ["30 Eyl", "Dijital hizmet vergisi, turizm payı, haberleşme vergisi ve belirli platform/kargo bildirimleri; 7440 sayılı Kanun 40. taksit."],
    ]
    other_table = Table(
        [[Paragraph(a, BODY), Paragraph(b, BODY)] for a, b in other_rows],
        colWidths=[28 * mm, 147 * mm],
    )
    other_table.setStyle(
        TableStyle(
            [
                ("GRID", (0, 0), (-1, -1), 0.5, LINE),
                ("BACKGROUND", (0, 0), (0, -1), PALE_BLUE),
                ("FONTNAME", (0, 0), (0, -1), "Arial-Bold"),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("TOPPADDING", (0, 0), (-1, -1), 7),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
            ]
        )
    )
    story += [
        other_table,
        Spacer(1, 7 * mm),
        section_label("Kaynak ve güncellik"),
        Spacer(1, 3 * mm),
        Paragraph(
            "Ana kaynak: <link href='https://cdn.gib.gov.tr/api/gibportal-file/file/getFileResources?objectKey=arsiv/onceki-dokumanlar/2026_vergi_takvimi.pdf' color='#1769AA'>GİB 2026 Yılı Vergi Takvimi</link> (Eylül sayfası).",
            BODY,
        ),
        Paragraph(
            "Canlı kontrol: <link href='https://gib.gov.tr/vergi-takvimi' color='#1769AA'>gib.gov.tr/vergi-takvimi</link> · Ba/Bs: <link href='https://www.gib.gov.tr/mevzuat/kanun/433/ozelge/38712' color='#1769AA'>GİB duyurusu</link>. Kaynaklar 22 Ağustos 2026 tarihinde kontrol edildi.",
            BODY,
        ),
        callout("Bu doküman bilgilendirme amaçlıdır. Resmî süre uzatımları ve size özel yükümlülükler için GİB duyurularını ve mali müşavirinizi esas alın.", PALE_ORANGE, ORANGE),
    ]
    make_doc(path, "Eylül 2026 Beyanname Takvimi").build(story, onFirstPage=footer, onLaterPages=footer)
    shutil.copy2(path, PUBLIC_DIR / name)
    return path


def qr_drawing(url: str, size: float = 34 * mm):
    code = qr.QrCodeWidget(url)
    bounds = code.getBounds()
    width = bounds[2] - bounds[0]
    height = bounds[3] - bounds[1]
    drawing = Drawing(size, size, transform=[size / width, 0, 0, size / height, 0, 0])
    drawing.add(code)
    return drawing


def build_first50():
    name = "systemcel-ilk-50-kampanya-detaylari.pdf"
    path = OUTPUT_DIR / name
    register_url = "https://systemcel.app/kayit?hesapTipi=Isletme&returnUrl=%2Fapp%2Fabonelik%3Fplan%3Disletme_buyume%26billing%3DYillik"
    story = [
        brand_band("LANSMAN KONTENJANI"),
        Spacer(1, 18 * mm),
        Paragraph("İlk 50 işletme.<br/>Yıllık ₺11.880.", TITLE),
        Table(
            [[Paragraph("<font color='#CBFF35'><b>Sonrası ₺15.480.</b></font>", ParagraphStyle("Price", parent=TITLE, fontSize=25, leading=29, textColor=WHITE))]],
            colWidths=[117 * mm],
            style=TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, -1), INK),
                    ("LEFTPADDING", (0, 0), (-1, -1), 9),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 9),
                    ("TOPPADDING", (0, 0), (-1, -1), 7),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
                ]
            ),
        ),
        Spacer(1, 6 * mm),
        Paragraph("Büyüme planı · yıllık peşin ödeme · fiyatlara KDV dahil değildir.", SUBTITLE),
        Spacer(1, 9 * mm),
        callout(
            "İlk 50 fiyatı, satın alınan ilk 12 aylık dönemin tamamına uygulanır. Yenilemede o tarihte geçerli liste fiyatı esas alınır.",
            PALE_LIME,
            LIME,
        ),
        Spacer(1, 12 * mm),
        section_label("Aylık eşdeğer ve ödeme karşılaştırması"),
        Spacer(1, 4 * mm),
    ]
    price_rows = [
        ["", "İlk 50", "Kontenjan sonrası", "Fark"],
        ["Yıllık bedel (+ KDV)", "₺11.880", "₺15.480", "₺3.600"],
        ["Aylık eşdeğer (+ KDV)", "₺990", "₺1.290", "₺300"],
        ["KDV (%20)", "₺2.376", "₺3.096", "₺720"],
        ["KDV dahil yıllık ödeme", "₺14.256", "₺18.576", "₺4.320"],
    ]
    price_table = Table(price_rows, colWidths=[55 * mm, 40 * mm, 48 * mm, 32 * mm], rowHeights=[11 * mm] + [12 * mm] * 4)
    price_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), LIME),
                ("FONTNAME", (0, 0), (-1, 0), "Arial-Bold"),
                ("FONTNAME", (0, 1), (0, -1), "Arial-Bold"),
                ("FONTNAME", (1, 1), (-1, -1), "Arial-Bold"),
                ("TEXTCOLOR", (1, 1), (1, -1), BLUE),
                ("BACKGROUND", (1, 1), (1, -1), PALE_BLUE),
                ("GRID", (0, 0), (-1, -1), 0.6, LINE),
                ("ALIGN", (1, 0), (-1, -1), "CENTER"),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ]
        )
    )
    story += [
        price_table,
        Spacer(1, 5 * mm),
        Paragraph("Aylık eşdeğer, yıllık bedelin 12’ye bölünmüş karşılığıdır; aylık taksit değildir. Yıllık bedel peşin tahsil edilir.", SMALL),
        PageBreak(),
        section_label("Kampanya koşulları · kısa ve açık"),
        Spacer(1, 5 * mm),
    ]
    conditions = [
        "Kampanya Büyüme planının yıllık peşin seçeneği içindir.",
        "İlk 50 işletme için yıllık bedel ₺11.880 + KDV’dir.",
        "Kontenjan dolduktan sonra yıllık liste bedeli ₺15.480 + KDV’dir.",
        "İlk 50 fiyatı, satın alınan 12 aylık abonelik döneminin tamamına uygulanır.",
        "Sonraki yenilemede, yenileme günündeki geçerli liste fiyatı esas alınır.",
        "Ödeme öncesinde abonelik koşulları ve tahsil edilecek toplam tutar ekranda ayrıca gösterilir.",
    ]
    story.append(checklist_section("Bilmeniz gerekenler", conditions))
    story += [
        Spacer(1, 5 * mm),
        section_label("Kayıt"),
        Spacer(1, 5 * mm),
        Table(
            [[
                Paragraph(
                    "<b>Büyüme · Yıllık</b><br/><br/>Kayıt ekranında planı ve yıllık dönemi tekrar kontrol edin.<br/><br/><link href='%s' color='#1769AA'><b>systemcel.app üzerinden kaydol →</b></link>" % register_url,
                    BODY,
                ),
                qr_drawing(register_url),
            ]],
            colWidths=[125 * mm, 45 * mm],
            style=TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, -1), PALE_BLUE),
                    ("BOX", (0, 0), (-1, -1), 0.7, BLUE),
                    ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                    ("LEFTPADDING", (0, 0), (-1, -1), 10),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 10),
                    ("TOPPADDING", (0, 0), (-1, -1), 10),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 10),
                ]
            ),
        ),
        Spacer(1, 6 * mm),
        callout("Bu dosyadaki tutarlar 22 Ağustos 2026 tarihli kampanya bilgisidir. Satın alma öncesinde ekrandaki güncel fiyat ve koşulları esas alın.", PALE_ORANGE, ORANGE),
    ]
    make_doc(path, "İlk 50 Kampanya Detayları").build(story, onFirstPage=footer, onLaterPages=footer)
    shutil.copy2(path, PUBLIC_DIR / name)
    return path


if __name__ == "__main__":
    outputs = [build_ai(), build_defter(), build_calendar(), build_first50()]
    for output in outputs:
        print(output)
