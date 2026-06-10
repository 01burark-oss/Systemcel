using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class TelegramCommandServiceTests
    {
        [Fact]
        public async Task ProcessUpdateAsync_OzetCommand_IncludesBusinessAndKalemBreakdown()
        {
            var now = DateTime.Now;
            var kasa = new FakeKasaService(new[]
            {
                new Kasa { Id = 1, Tarih = now.AddDays(-1), Tip = "Gelir", Tutar = 120m, OdemeYontemi = "Nakit", Kalem = "Nakit Satis" },
                new Kasa { Id = 2, Tarih = now.AddDays(-1), Tip = "Gelir", Tutar = 80m, OdemeYontemi = "KrediKarti", Kalem = "Nakit Satis" },
                new Kasa { Id = 3, Tarih = now.AddDays(-1), Tip = "Gider", Tutar = 50m, OdemeYontemi = "Havale", Kalem = "Kira", GiderTuru = "Kira" }
            });

            var summary = new FakeSummaryService
            {
                SummaryToReturn = new PeriodSummary
                {
                    IncomeTotal = 200m,
                    ExpenseTotal = 50m,
                    IncomeCount = 2,
                    ExpenseCount = 1
                }
            };

            var kalem = new FakeKalemTanimiService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, _, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 1,
                ChatId = 123,
                UserId = 42,
                Text = "/ozet 7"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("İşletme: Demo Isletme", text!);
            Assert.Contains("Ödeme Yöntemleri:", text!);
            Assert.Contains("- Nakit:", text!);
            Assert.Contains("- Kredi Kartı:", text!);
            Assert.Contains("- Online Ödeme:", text!);
            Assert.Contains("- Havale:", text!);
            Assert.Contains("Gelir Kalemleri:", text!);
            Assert.Contains("Gider Kalemleri:", text!);
            Assert.Contains("- Nakit Satis:", text!);
            Assert.Contains("- Kira:", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_GelirCommand_ResponseContainsBusinessAndKalem()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gelir", Ad = "Genel Gelir" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, _, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 2,
                ChatId = 123,
                UserId = 42,
                Text = "/gelir 125"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("Kaydedildi. Id:", text!);
            Assert.Contains("İşletme: Demo Isletme", text!);
            Assert.Contains("Tip: Gelir", text!);
            Assert.Contains("Kalem: Genel Gelir", text!);

            Assert.NotNull(kasa.LastCreated);
            Assert.Equal("Gelir", kasa.LastCreated!.Tip);
            Assert.Equal("Genel Gelir", kasa.LastCreated.Kalem);
        }

        [Fact]
        public async Task ProcessUpdateAsync_SifreCommand_IsRejected()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService();
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, security, _, _) = BuildService(kasa, kalem, summary, isletme);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 3,
                ChatId = 123,
                UserId = 42,
                Text = "/sifre 2468"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Equal("0000", security.Pin);
            Assert.Contains("PIN değişimi Telegram üzerinden kapatıldı.", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_StartWithPairingCode_CompletesPairingBeforeTargetChatCheck()
        {
            var pairing = new FakeTelegramPairingService();
            var (_, handler, service, _, _, _) = BuildService(
                new FakeKasaService(),
                new FakeKalemTanimiService(),
                new FakeSummaryService(),
                new FakeIsletmeService(),
                configureSettings: settings =>
                {
                    settings.ChatId = string.Empty;
                    settings.AllowedUserIds = string.Empty;
                },
                pairingService: pairing);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 4,
                ChatId = 987654,
                UserId = 456,
                Text = "/start SC-123456"
            });

            Assert.Equal("SC-123456", pairing.LastCode);
            Assert.Equal(987654, pairing.LastChatId);
            Assert.Equal(456, pairing.LastUserId);
            Assert.Equal("pairing-ok", handler.GetLastFormFieldValue("/sendMessage", "text"));
        }

        [Fact]
        public async Task ProcessUpdateAsync_UnpairedStartWithoutCode_AsksForAppCode()
        {
            var (_, handler, service, _, _, _) = BuildService(
                new FakeKasaService(),
                new FakeKalemTanimiService(),
                new FakeSummaryService(),
                new FakeIsletmeService(),
                configureSettings: settings =>
                {
                    settings.ChatId = string.Empty;
                    settings.AllowedUserIds = string.Empty;
                });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 5,
                ChatId = 987654,
                UserId = 456,
                Text = "/start"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Ayarlar > Telegram", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_FollowsPromptAndCreatesGroupedExpenseRecords()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Mutfak Giderleri" },
                new KalemTanimi { Id = 3, Tip = "Gider", Ad = "Temizlik Giderleri" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Migros",
                ReceiptDate = new DateTime(2026, 3, 29, 10, 30, 0),
                PaymentMethod = "KrediKarti",
                ReceiptTotal = 60m,
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Sut", Amount = 40m, CandidateKalem = "Mutfak Giderleri", Confidence = 0.97m, NeedsUserInput = false },
                    new() { RawName = "Deterjan", Amount = 20m, CandidateKalem = "", Confidence = 0.12m, NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 10,
                MessageId = 100,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-1"
            });

            var prompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Ürün için gider kalemi seç: Deterjan", prompt!);
            Assert.Contains("Kalemler genel gider grupları olmalı", prompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 11,
                ChatId = 123,
                UserId = 42,
                Text = "3"
            });

            var summaryText = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fiş özeti", summaryText!);
            Assert.Contains("Ödeme: KrediKarti", summaryText!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 12,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            Assert.Equal(2, kasa.Rows.Count);
            Assert.Contains(kasa.Rows, x => x.Kalem == "Mutfak Giderleri" && x.Tutar == 40m);
            Assert.Contains(kasa.Rows, x => x.Kalem == "Temizlik Giderleri" && x.Tutar == 20m);
            Assert.All(kasa.Rows, x => Assert.Equal("OCR Fiş | Migros", x.Aciklama));
            Assert.All(kasa.Rows, x => Assert.DoesNotContain("Sut", x.Aciklama));
            Assert.All(kasa.Rows, x => Assert.DoesNotContain("Deterjan", x.Aciklama));
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_UsesDefaultGeneralCategoriesWhenNoExpenseCategoryExists()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService();
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Bim",
                ReceiptDate = new DateTime(2026, 3, 29, 12, 0, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Belirsiz Urun", Amount = 10m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 19,
                MessageId = 190,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-defaults"
            });

            var prompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Mutfak Giderleri", prompt!);
            Assert.Contains("Personel Giderleri", prompt!);
            Assert.Contains("Kalemler genel gider grupları olmalı", prompt!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_WhenGeminiRateLimited_ShowsSpecificError()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextException = new InvalidOperationException("Gemini OCR failed: 429 - rate limit");

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 19,
                MessageId = 191,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-rate-limit"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fiş OCR işlenemedi.", text!);
            Assert.Contains("Gemini kota veya hız limiti aşıldı.", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_NewCategoryFlow_CreatesCategoryAndUsesIt()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Mutfak Giderleri" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "Carrefour",
                ReceiptDate = new DateTime(2026, 3, 29, 18, 15, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Paspas", Amount = 55m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 20,
                MessageId = 200,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-2"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 21,
                ChatId = 123,
                UserId = 42,
                Text = "yeni: Sarf Giderleri"
            });

            var confirmText = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("'Sarf Giderleri' adlı gider kalemi oluşturayım mı?", confirmText!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 22,
                ChatId = 123,
                UserId = 42,
                Text = "evet"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 23,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            var allCategories = await kalem.GetAllAsync();
            Assert.Contains(allCategories, x => x.Tip == "Gider" && x.Ad == "Sarf Giderleri");
            Assert.Contains(kasa.Rows, x => x.Kalem == "Sarf Giderleri" && x.Tutar == 55m);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoReceipt_AsksForDateAndPaymentWhenMissing()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Market" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "A101",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Ekmek", Amount = 15m, CandidateKalem = "Market", NeedsUserInput = false }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 30,
                MessageId = 300,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-3"
            });

            var datePrompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fiş tarihi eksik", datePrompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 31,
                ChatId = 123,
                UserId = 42,
                Text = "atla"
            });

            var paymentPrompt = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Ödeme yöntemi eksik", paymentPrompt!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 32,
                ChatId = 123,
                UserId = 42,
                Text = "Havale"
            });

            var finalSummary = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Fiş özeti", finalSummary!);
            Assert.Contains("Ödeme: Havale", finalSummary!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_WhenSessionActive_RejectsSecondPhoto()
        {
            var kasa = new FakeKasaService();
            var kalem = new FakeKalemTanimiService(new[]
            {
                new KalemTanimi { Id = 1, Tip = "Gider", Ad = "Genel Gider" },
                new KalemTanimi { Id = 2, Tip = "Gider", Ad = "Market" }
            });
            var summary = new FakeSummaryService();
            var isletme = new FakeIsletmeService
            {
                Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
            };

            var (_, handler, service, _, ocr, _) = BuildService(kasa, kalem, summary, isletme, BuildPhotoResponder());
            ocr.NextResult = new ReceiptOcrResult
            {
                Merchant = "A101",
                ReceiptDate = new DateTime(2026, 3, 29, 9, 0, 0),
                PaymentMethod = "Nakit",
                Items = new List<ReceiptOcrLineItem>
                {
                    new() { RawName = "Belirsiz", Amount = 10m, CandidateKalem = "", NeedsUserInput = true }
                }
            };

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 40,
                MessageId = 400,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-4"
            });

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 41,
                MessageId = 401,
                ChatId = 123,
                UserId = 42,
                PhotoFileId = "photo-5"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Devam eden bir fiş oturumu var", text!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_StokTextCommand_KnownBarcodeRequiresConfirmationAndCreatesMovement()
        {
            var product = new UrunHizmet
            {
                Id = 5,
                Ad = "Kola",
                Barkod = "8690000000000",
                Birim = "Adet",
                Aktif = true
            };

            var (_, handler, service, _, stock, _, _, _) = BuildServiceWithStock(new[] { product });
            stock.SetCurrentStock(product.Id, 12m);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 50,
                ChatId = 123,
                UserId = 42,
                Text = "/stok 8690000000000 +10"
            });

            var summary = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Stok hareketi özeti", summary!);
            Assert.Contains("Ürün: Kola", summary!);
            Assert.Empty(stock.Requests);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 51,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            Assert.Single(stock.Requests);
            Assert.Equal(product.Id, stock.LastRequest!.UrunHizmetId);
            Assert.Equal(10m, stock.LastRequest.Miktar);
            Assert.Equal("Telegram", stock.LastRequest.Kaynak);
            Assert.Equal("Telegram stok | Barkod: 8690000000000", stock.LastRequest.Aciklama);

            var result = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Stok hareketi kaydedildi.", result!);
            Assert.Contains("Mevcut stok: 22", result!);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoWithStockCaption_UsesBarcodeFlowAndSkipsReceiptOcr()
        {
            var product = new UrunHizmet
            {
                Id = 6,
                Ad = "Biskuvit",
                Barkod = "8691111111111",
                Birim = "Adet",
                Aktif = true
            };

            var (_, handler, service, _, stock, barcode, _, ocr) =
                BuildServiceWithStock(new[] { product }, BuildPhotoResponder());
            barcode.NextResult = BarcodeReadResult.Found("8691111111111");
            stock.SetCurrentStock(product.Id, 3m);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 52,
                MessageId = 520,
                ChatId = 123,
                UserId = 42,
                Caption = "/stok +50",
                PhotoFileId = "photo-stock-1"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Stok hareketi özeti", text!);
            Assert.Contains("Ürün: Biskuvit", text!);
            Assert.Null(ocr.LastRequest);
            Assert.False(string.IsNullOrWhiteSpace(barcode.LastImagePath));
            Assert.Empty(stock.Requests);
        }

        [Fact]
        public async Task ProcessUpdateAsync_PhotoWithStockCaption_WhenBarcodeUnreadable_AsksManualBarcode()
        {
            var (_, handler, service, _, stock, barcode, stockSessionStore, _) =
                BuildServiceWithStock(responder: BuildPhotoResponder());
            barcode.NextResult = BarcodeReadResult.Failed("not found");

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 53,
                MessageId = 530,
                ChatId = 123,
                UserId = 42,
                Caption = "/stok +50",
                PhotoFileId = "photo-stock-2"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Barkod okunamadı", text!);
            Assert.NotNull(stockSessionStore.LastSaved);
            Assert.Equal(StockSessionStep.AwaitBarcode, stockSessionStore.LastSaved!.Step);
            Assert.Equal(50m, stockSessionStore.LastSaved.PendingQuantity);
            Assert.Empty(stock.Requests);
        }

        [Fact]
        public async Task ProcessUpdateAsync_StokUnknownBarcode_AsksProductInfoThenCreatesProductAndMovement()
        {
            var (_, handler, service, products, stock, _, _, _) = BuildServiceWithStock();

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 54,
                ChatId = 123,
                UserId = 42,
                Text = "/stok 8692222222222 +5"
            });

            Assert.Contains("Bu barkod kayıtlı değil", handler.GetLastFormFieldValue("/sendMessage", "text")!);

            foreach (var (updateId, answer) in new[]
                     {
                         (55, "Yeni Urun"),
                         (56, "Adet"),
                         (57, "20"),
                         (58, "10,5"),
                         (59, "15"),
                         (60, "2")
                     })
            {
                await service.ProcessUpdateAsync(new TelegramUpdate
                {
                    UpdateId = updateId,
                    ChatId = 123,
                    UserId = 42,
                    Text = answer
                });
            }

            var summary = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Yeni ürün: Yeni Urun", summary!);
            Assert.Empty(products.Rows);
            Assert.Empty(stock.Requests);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 61,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            Assert.Single(products.Rows);
            Assert.Equal("Yeni Urun", products.Rows[0].Ad);
            Assert.Equal("8692222222222", products.Rows[0].Barkod);
            Assert.Equal(10.5m, products.Rows[0].AlisFiyati);
            Assert.Single(stock.Requests);
            Assert.Equal(products.Rows[0].Id, stock.LastRequest!.UrunHizmetId);
            Assert.Equal(5m, stock.LastRequest.Miktar);
        }

        [Fact]
        public async Task ProcessUpdateAsync_StokCancel_RemovesSessionAndDoesNotCreateMovement()
        {
            var (_, handler, service, _, stock, _, stockSessionStore, _) = BuildServiceWithStock();

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 62,
                ChatId = 123,
                UserId = 42,
                Text = "/stok +7"
            });

            Assert.NotNull(stockSessionStore.LastSaved);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 63,
                ChatId = 123,
                UserId = 42,
                Text = "iptal"
            });

            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Stok oturumu iptal edildi.", text!);
            Assert.Empty(stock.Requests);
            Assert.Null(await stockSessionStore.GetAsync(123, 42));
        }

        [Fact]
        public async Task ProcessUpdateAsync_StokNegativeMovement_WarnsButAllowsConfirmation()
        {
            var product = new UrunHizmet
            {
                Id = 7,
                Ad = "Soda",
                Barkod = "8693333333333",
                Birim = "Adet",
                Aktif = true
            };

            var (_, handler, service, _, stock, _, _, _) = BuildServiceWithStock(new[] { product });
            stock.SetCurrentStock(product.Id, 2m);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 64,
                ChatId = 123,
                UserId = 42,
                Text = "/stok 8693333333333 -3"
            });

            var summary = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("eksiye düşürecek", summary!);

            await service.ProcessUpdateAsync(new TelegramUpdate
            {
                UpdateId = 65,
                ChatId = 123,
                UserId = 42,
                Text = "onayla"
            });

            Assert.Single(stock.Requests);
            Assert.Equal(-3m, stock.LastRequest!.Miktar);

            var result = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("Mevcut stok: -1", result!);
            Assert.Contains("eksiye düştü", result!);
        }

        private static (TelegramBotService Bot, RecordingHttpMessageHandler Handler, TelegramCommandService Service, FakeAppSecurityService Security, FakeReceiptOcrService Ocr, FakeTelegramReceiptSessionStore SessionStore) BuildService(
            FakeKasaService kasa,
            FakeKalemTanimiService kalem,
            FakeSummaryService summary,
            FakeIsletmeService isletme,
            Func<HttpRequestMessage, string, HttpResponseMessage>? responder = null,
            Action<TelegramSettings>? configureSettings = null,
            ITelegramPairingService? pairingService = null)
        {
            var handler = new RecordingHttpMessageHandler(responder);
            var http = new HttpClient(handler);
            var bot = new TelegramBotService(http, "test-token");
            var settings = new TelegramSettings
            {
                BotToken = "test-token",
                ChatId = "123",
                EnableCommands = true,
                AllowedUserIds = "42"
            };
            configureSettings?.Invoke(settings);

            var backup = new BackupReportService(
                bot,
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_tests_{Guid.NewGuid():N}.db"))));

            var security = new FakeAppSecurityService();
            var approvals = new FakeTelegramApprovalService();
            var ocr = new FakeReceiptOcrService();
            var sessionStore = new FakeTelegramReceiptSessionStore();
            var products = new FakeUrunHizmetService();
            var stock = new FakeStokService();
            var barcode = new FakeBarcodeReaderService();
            var stockSessionStore = new FakeTelegramStockSessionStore();
            var receiptOcrSettings = new ReceiptOcrSettings
            {
                Provider = "Gemini",
                ApiKey = "test-key",
                Model = "gemini-2.5-flash",
                SessionTimeoutMinutes = 30
            };

            var service = new TelegramCommandService(
                bot,
                settings,
                kasa,
                kalem,
                summary,
                isletme,
                security,
                backup,
                approvals,
                ocr,
                sessionStore,
                receiptOcrSettings,
                products,
                stock,
                barcode,
                stockSessionStore,
                pairingService);

            return (bot, handler, service, security, ocr, sessionStore);
        }

        private static (
            TelegramBotService Bot,
            RecordingHttpMessageHandler Handler,
            TelegramCommandService Service,
            FakeUrunHizmetService Products,
            FakeStokService Stock,
            FakeBarcodeReaderService Barcode,
            FakeTelegramStockSessionStore StockSessionStore,
            FakeReceiptOcrService Ocr) BuildServiceWithStock(
                IEnumerable<UrunHizmet>? productsSeed = null,
                Func<HttpRequestMessage, string, HttpResponseMessage>? responder = null)
        {
            var handler = new RecordingHttpMessageHandler(responder);
            var http = new HttpClient(handler);
            var bot = new TelegramBotService(http, "test-token");
            var settings = new TelegramSettings
            {
                BotToken = "test-token",
                ChatId = "123",
                EnableCommands = true,
                AllowedUserIds = "42"
            };

            var backup = new BackupReportService(
                bot,
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_tests_{Guid.NewGuid():N}.db"))));

            var security = new FakeAppSecurityService();
            var approvals = new FakeTelegramApprovalService();
            var ocr = new FakeReceiptOcrService();
            var receiptSessionStore = new FakeTelegramReceiptSessionStore();
            var receiptOcrSettings = new ReceiptOcrSettings
            {
                Provider = "Gemini",
                ApiKey = "test-key",
                Model = "gemini-2.5-flash",
                SessionTimeoutMinutes = 30
            };
            var products = new FakeUrunHizmetService(productsSeed);
            var stock = new FakeStokService();
            var barcode = new FakeBarcodeReaderService();
            var stockSessionStore = new FakeTelegramStockSessionStore();

            var service = new TelegramCommandService(
                bot,
                settings,
                new FakeKasaService(),
                new FakeKalemTanimiService(),
                new FakeSummaryService(),
                new FakeIsletmeService
                {
                    Active = new Isletme { Id = 7, Ad = "Demo Isletme", IsAktif = true }
                },
                security,
                backup,
                approvals,
                ocr,
                receiptSessionStore,
                receiptOcrSettings,
                products,
                stock,
                barcode,
                stockSessionStore);

            return (bot, handler, service, products, stock, barcode, stockSessionStore, ocr);
        }

        private static Func<HttpRequestMessage, string, HttpResponseMessage> BuildPhotoResponder()
        {
            return (request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getFile", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{\"file_path\":\"photos/test.jpg\"}}");
                }

                if (url.Contains("/file/bottest-token/photos/test.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
                    };
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{}}");
            };
        }

        private sealed class FakeTelegramPairingService : ITelegramPairingService
        {
            public string LastCode { get; private set; } = string.Empty;
            public long LastChatId { get; private set; }
            public long? LastUserId { get; private set; }

            public TelegramPairingCode EnsureActiveCode()
            {
                return new TelegramPairingCode("SC-123456", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10));
            }

            public TelegramPairingCode RenewCode()
            {
                return EnsureActiveCode();
            }

            public bool TryCompletePairing(string code, long chatId, long? userId, out string message)
            {
                LastCode = code;
                LastChatId = chatId;
                LastUserId = userId;
                message = "pairing-ok";
                return string.Equals(code, "SC-123456", StringComparison.OrdinalIgnoreCase);
            }

            public void ClearPairing()
            {
            }
        }
    }
}
