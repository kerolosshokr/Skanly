// Skanly.Infrastructure/Pdf/ContractDocument.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Skanly.Application.Features.Contracts.DTOs;

namespace Skanly.Infrastructure.Pdf;

/// <summary>
/// QuestPDF document definition for the Skanly rental contract.
/// Layout: A4, bilingual (Arabic right-aligned, English left-aligned),
/// professional styling matching the Skanly brand.
/// </summary>
public class ContractDocument : IDocument
{
    private readonly GenerateContractDto _data;
    private readonly ContractSettings _settings;

    // Brand colors
    private static readonly string PrimaryColor = "#6C63FF";
    private static readonly string DarkColor = "#1f2937";
    private static readonly string MediumColor = "#6b7280";
    private static readonly string LightColor = "#f9fafb";
    private static readonly string BorderColor = "#e5e7eb";
    private static readonly string AccentColor = "#10b981";

    public ContractDocument(
        GenerateContractDto data,
        ContractSettings settings)
    {
        _data = data;
        _settings = settings;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Rental Contract — {_data.ContractNumber}",
        Author = _settings.CompanyNameEn,
        Subject = $"Student Rental Agreement — {_data.PropertyTitle}",
        Keywords = "Skanly, Rental, Contract, Egypt, Student Housing",
        Creator = "Skanly Platform",
        Producer = "Skanly Platform via QuestPDF",
        CreationDate = _data.GeneratedAt
    };

    public DocumentSettings GetSettings() => new()
    {
        PDFA_Conformance = PDFA_Conformance.None,
        ImageCompressionQuality = ImageCompressionQuality.High
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x
                .FontSize(10)
                .FontColor(DarkColor));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER ────────────────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Brand banner
            col.Item().Background(PrimaryColor).Padding(16).Row(row =>
            {
                // Platform name + logo placeholder
                row.RelativeItem().Column(inner =>
                {
                    inner.Item()
                        .Text(_settings.CompanyNameEn)
                        .FontSize(22)
                        .FontColor(Colors.White)
                        .Bold();

                    inner.Item()
                        .Text(_settings.CompanyNameAr)
                        .FontSize(14)
                        .FontColor(Colors.White)
                        .Light();
                });

                // Contract meta (right side)
                row.ConstantItem(180).Column(inner =>
                {
                    inner.Item().AlignRight().Text(text =>
                    {
                        text.Span("CONTRACT")
                            .FontColor(Colors.White)
                            .FontSize(9)
                            .Bold()
                            .LetterSpacing(0.12f);
                    });

                    inner.Item().AlignRight().Text(text =>
                    {
                        text.Span(_data.ContractNumber)
                            .FontColor(Colors.White)
                            .FontSize(13)
                            .Bold();
                    });

                    inner.Item().AlignRight().PaddingTop(4).Text(text =>
                    {
                        text.Span(_data.GeneratedAt.ToString("MMMM dd, yyyy"))
                            .FontColor(Colors.White)
                            .FontSize(9)
                            .Light();
                    });
                });
            });

            // Title bar
            col.Item().Background(LightColor)
                .BorderBottom(1).BorderColor(BorderColor)
                .PaddingVertical(10).PaddingHorizontal(16)
                .Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("STUDENT RENTAL AGREEMENT")
                            .FontSize(12)
                            .Bold()
                            .FontColor(DarkColor);
                    });

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("عقد إيجار سكن طلابي")
                            .FontSize(12)
                            .Bold()
                            .FontColor(DarkColor);
                    });
                });

            col.Item().Height(8);
        });
    }

    // ── CONTENT ───────────────────────────────────────────────────────────────

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(4).Column(col =>
        {
            // Introduction clause
            col.Item().Element(ComposePreamble);
            col.Item().Height(12);

            // Section 1: Parties
            col.Item().Element(ComposePartiesSection);
            col.Item().Height(12);

            // Section 2: Property Details
            col.Item().Element(ComposePropertySection);
            col.Item().Height(12);

            // Section 3: Financial Terms
            col.Item().Element(ComposeFinancialSection);
            col.Item().Height(12);

            // Section 4: Rental Period
            col.Item().Element(ComposeDateSection);
            col.Item().Height(12);

            // Section 5: Terms & Conditions
            col.Item().Element(ComposeTermsSection);
            col.Item().Height(12);

            // Section 6: Platform Role
            col.Item().Element(ComposePlatformSection);
            col.Item().Height(16);

            // Signature block
            col.Item().Element(ComposeSignatureSection);
        });
    }

    // ── PREAMBLE ──────────────────────────────────────────────────────────────

    private void ComposePreamble(IContainer container)
    {
        container.Background(LightColor)
            .Border(1).BorderColor(BorderColor)
             .CornerRadius(4)
            .Padding(12)
            .Text(text =>
            {
                text.Span(
                    "This Rental Agreement (\"Contract\") is entered into on ")
                    .FontSize(9).FontColor(MediumColor);

                text.Span(_data.GeneratedAt.ToString("MMMM dd, yyyy"))
                    .Bold().FontSize(9).FontColor(DarkColor);

                text.Span(
                    " between the parties listed below, facilitated through the ")
                    .FontSize(9).FontColor(MediumColor);

                text.Span(_settings.CompanyNameEn)
                    .Bold().FontSize(9).FontColor(PrimaryColor);

                text.Span(
                    " digital platform. This contract is generated automatically " +
                    "upon confirmed payment and constitutes a legally binding " +
                    "agreement between the Tenant and the Property Owner under " +
                    "Egyptian tenancy law.")
                    .FontSize(9).FontColor(MediumColor);
            });
    }

    // ── SECTION HEADER HELPER ─────────────────────────────────────────────────

    private void SectionHeader(
        IContainer container, string number, string titleEn, string titleAr)
    {
         container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Section number circle
                row.ConstantItem(26).Height(22)
                    .Background(PrimaryColor)
                    .CornerRadius(4)
                    .AlignCenter().AlignMiddle()
                    .Text(number)
                    .Bold()
                    .FontSize(11)
                    .FontColor(Colors.White);

                row.ConstantItem(8);

                // English title
                row.RelativeItem().AlignMiddle()
                    .Text(titleEn)
                    .FontSize(11)
                    .Bold()
                    .FontColor(DarkColor);

                // Arabic title
                row.RelativeItem().AlignRight().AlignMiddle()
                    .Text(titleAr)
                    .FontSize(11)
                    .Bold()
                    .FontColor(DarkColor);
            });

            col.Item().Height(2).Background("#CFCBFF");
            col.Item().Height(8);
        });
    }

    private void InfoRow(
        IContainer container,
        string label,
        string value,
        string? labelAr = null)
    {
         container.Row(row =>
        {
            row.ConstantItem(160).Text(text =>
            {
                text.Span(label).FontSize(9)
                    .FontColor(MediumColor).Bold();
                if (labelAr is not null)
                    text.Span($" / {labelAr}")
                        .FontSize(8).FontColor(MediumColor).Light();
            });

            row.RelativeItem().Text(value)
                .FontSize(9).FontColor(DarkColor);
        });
    }

    // ── SECTION 1: PARTIES ────────────────────────────────────────────────────

    private void ComposePartiesSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "1", "Contracting Parties", "أطراف العقد"));

            // Two-column layout: Tenant | Owner
            col.Item().Row(row =>
            {
                // Tenant
                row.RelativeItem().Border(1).BorderColor(BorderColor)
                    .CornerRadius(4).Padding(12).Column(inner =>
                    {
                        inner.Item().Text("TENANT / المستأجر")
                            .FontSize(9).Bold()
                            .FontColor(PrimaryColor);

                        inner.Item().Height(6);
                        inner.Item().Element(c => InfoRow(c,
                            "Full Name / الاسم",
                            _data.StudentFullName));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "National ID / رقم القومي",
                            _data.StudentNationalId));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "Phone / الهاتف",
                            _data.StudentPhone ?? "—"));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "Email / البريد",
                            _data.StudentEmail));

                        if (!string.IsNullOrEmpty(
                            _data.StudentUniversityNameEn))
                        {
                            inner.Item().Height(4);
                            inner.Item().Element(c => InfoRow(c,
                                "University / الجامعة",
                                _data.StudentUniversityNameEn));
                        }
                    });

                row.ConstantItem(12);

                // Owner
                row.RelativeItem().Border(1).BorderColor(BorderColor)
                    .CornerRadius(4).Padding(12).Column(inner =>
                    {
                        inner.Item().Text("OWNER / المالك")
                            .FontSize(9).Bold()
                            .FontColor(AccentColor);

                        inner.Item().Height(6);
                        inner.Item().Element(c => InfoRow(c,
                            "Full Name / الاسم",
                            _data.OwnerFullName));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "National ID / رقم القومي",
                            _data.OwnerNationalId));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "Phone / الهاتف",
                            _data.OwnerPhone ?? "—"));

                        inner.Item().Height(4);
                        inner.Item().Element(c => InfoRow(c,
                            "Email / البريد",
                            _data.OwnerEmail));

                        if (!string.IsNullOrEmpty(_data.OwnerBusinessName))
                        {
                            inner.Item().Height(4);
                            inner.Item().Element(c => InfoRow(c,
                                "Business / التجاري",
                                _data.OwnerBusinessName));
                        }
                    });
            });
        });
    }

    // ── SECTION 2: PROPERTY ───────────────────────────────────────────────────

    private void ComposePropertySection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "2", "Property Details", "تفاصيل العقار"));

            col.Item().Border(1).BorderColor(BorderColor)
                .CornerRadius(4).Padding(12).Column(inner =>
                {
                    // Property title in accent bar
                    inner.Item().Background(LightColor)
                        .CornerRadius(4).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text("Property / العقار:")
                                .FontSize(9).FontColor(MediumColor);

                            row.RelativeItem().AlignRight()
                                .Text(_data.PropertyTitle)
                                .FontSize(10).Bold().FontColor(DarkColor);
                        });

                    inner.Item().Height(8);

                    // Property details grid
                    inner.Item().Row(row =>
                    {
                        row.RelativeItem().Column(leftCol =>
                        {
                            leftCol.Item().Element(c => InfoRow(c,
                                "Address / العنوان",
                                _data.PropertyAddress));

                            leftCol.Item().Height(4);
                            leftCol.Item().Element(c => InfoRow(c,
                                "Area / المنطقة",
                                _data.AreaNameEn));

                            leftCol.Item().Height(4);
                            leftCol.Item().Element(c => InfoRow(c,
                                "Type / النوع",
                                _data.PropertyTypeDisplay));
                        });

                        row.ConstantItem(20);

                        row.RelativeItem().Column(rightCol =>
                        {
                            rightCol.Item().Element(c => InfoRow(c,
                                "Rooms / الغرف",
                                _data.Rooms.ToString()));

                            rightCol.Item().Height(4);
                            rightCol.Item().Element(c => InfoRow(c,
                                "Beds / الأسرة",
                                _data.Beds.ToString()));

                            rightCol.Item().Height(4);
                            rightCol.Item().Element(c => InfoRow(c,
                                "Gender Policy / السياسة",
                                _data.GenderPolicyDisplay));
                        });
                    });

                    // Amenities
                    if (_data.AmenityNames.Any())
                    {
                        inner.Item().Height(8);
                        inner.Item().Text("Amenities / المرافق:")
                            .FontSize(9).FontColor(MediumColor).Bold();

                        inner.Item().Height(4);
                        inner.Item().Row(amenityRow =>
                        {
                            foreach (var amenity in _data.AmenityNames)
                            {
                                amenityRow.AutoItem()
                                    .PaddingRight(6).PaddingBottom(4)
                                    .Background(LightColor)
                                    .Border(1).BorderColor(BorderColor)
                                    .CornerRadius(20)
                                    .PaddingHorizontal(8).PaddingVertical(2)
                                    .Text(amenity)
                                    .FontSize(8)
                                    .FontColor(DarkColor);
                            }
                        });
                    }
                });
        });
    }

    // ── SECTION 3: FINANCIAL TERMS ────────────────────────────────────────────

    private void ComposeFinancialSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "3", "Financial Terms", "الشروط المالية"));

            col.Item().Border(1).BorderColor(BorderColor)
                .CornerRadius(4).Padding(0).Column(inner =>
                {
                    // Financial table header
                    inner.Item().Background(DarkColor)
                        .PaddingVertical(8).PaddingHorizontal(12)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Description / البيان")
                                .FontSize(9).Bold()
                                .FontColor(Colors.White);

                            row.ConstantItem(120).AlignRight()
                                .Text("Amount (EGP) / المبلغ")
                                .FontSize(9).Bold()
                                .FontColor(Colors.White);
                        });

                    // Monthly rent row
                    FinancialTableRow(inner,
                        "Monthly Rent / الإيجار الشهري",
                        _data.MonthlyRent,
                        isHighlighted: false);

                    // Deposit row
                    FinancialTableRow(inner,
                        "Security Deposit (20%) / تأمين",
                        _data.DepositAmount,
                        isHighlighted: false);

                    // Platform commission (owner-facing info)
                    FinancialTableRow(inner,
                        "Platform Commission / عمولة المنصة" +
                        $" ({_data.CommissionRate}%)",
                        _data.CommissionAmount,
                        isHighlighted: false,
                        note: "Paid by owner to platform");

                    // Total due at signing
                    FinancialTableRow(inner,
                        "PAID — Deposit Confirmed / تم الدفع",
                        _data.DepositAmount,
                        isHighlighted: true);

                    // Payment reference
                    inner.Item().Background(LightColor)
                        .PaddingVertical(8).PaddingHorizontal(12)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Payment Method / طريقة الدفع")
                                .FontSize(9).FontColor(MediumColor);

                            row.RelativeItem().AlignRight()
                                .Text(_data.PaymentMethod)
                                .FontSize(9).Bold()
                                .FontColor(DarkColor);
                        });

                    if (!string.IsNullOrEmpty(_data.TransactionReference))
                    {
                        inner.Item()
                            .PaddingVertical(8).PaddingHorizontal(12)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Text("Transaction Reference / مرجع العملية")
                                    .FontSize(9).FontColor(MediumColor);

                                row.RelativeItem().AlignRight()
                                    .Text(_data.TransactionReference)
                                    .FontSize(9).FontColor(DarkColor)
                                    .FontFamily(Fonts.CourierNew);
                            });
                    }
                });
        });
    }

    private void FinancialTableRow(
        ColumnDescriptor col,
        string label,
        decimal amount,
        bool isHighlighted,
        string? note = null)
    {
        col.Item()
            .Background(isHighlighted ? "#f0fdf4" : Colors.White)
            .BorderTop(1).BorderColor(BorderColor)
            .PaddingVertical(8).PaddingHorizontal(12)
            .Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                  var text =  inner.Item().Text(label)
                        .FontSize(9)
                        
                        .FontColor(isHighlighted ? AccentColor : DarkColor);

                    if (note is not null)
                        inner.Item().Text(note)
                            .FontSize(8).FontColor(MediumColor).Light();
                });

                row.ConstantItem(120).AlignRight()
                    .Text($"EGP {amount:N2}")
                    .FontSize(isHighlighted ? 11 : 9)
                    
                    .FontColor(isHighlighted ? AccentColor : DarkColor);
            });
    }

    // ── SECTION 4: RENTAL PERIOD ──────────────────────────────────────────────

    private void ComposeDateSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "4", "Rental Period", "مدة الإيجار"));

            col.Item().Border(1).BorderColor(BorderColor)
                .CornerRadius(4).Padding(12).Row(row =>
                {
                    // Check-in
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("CHECK-IN / بداية الإيجار")
                            .FontSize(8).FontColor(MediumColor).Bold();

                        inner.Item().Height(4);
                        inner.Item().Text(
                            _data.CheckInDate.ToString("MMMM dd, yyyy"))
                            .FontSize(16).Bold()
                            .FontColor(PrimaryColor);

                        inner.Item()
                            .Text(_data.CheckInDate.ToString("dddd"))
                            .FontSize(9).FontColor(MediumColor);
                    });

                    // Duration indicator
                    row.ConstantItem(60).AlignCenter().AlignMiddle()
                        .Column(inner =>
                        {
                            inner.Item().AlignCenter()
                                .Text("→")
                                .FontSize(18)
                                .FontColor(BorderColor);

                            if (_data.CheckOutDate.HasValue)
                            {
                                var days = _data.CheckOutDate.Value
                                    .DayNumber -
                                    _data.CheckInDate.DayNumber;
                                var months = Math.Round(days / 30.0, 1);

                                inner.Item().AlignCenter()
                                    .Text($"{months}mo")
                                    .FontSize(8)
                                    .FontColor(MediumColor);
                            }
                        });

                    // Check-out
                    row.RelativeItem().AlignRight().Column(inner =>
                    {
                        inner.Item().AlignRight()
                            .Text("CHECK-OUT / نهاية الإيجار")
                            .FontSize(8).FontColor(MediumColor).Bold();

                        inner.Item().Height(4);
                        inner.Item().AlignRight().Text(
                            _data.CheckOutDate.HasValue
                                ? _data.CheckOutDate.Value.ToString("MMMM dd, yyyy")
                                : "Open-ended / مفتوح")
                            .FontSize(16).Bold()
                            .FontColor(DarkColor);

                        if (_data.CheckOutDate.HasValue)
                            inner.Item().AlignRight()
                                .Text(_data.CheckOutDate.Value.ToString("dddd"))
                                .FontSize(9).FontColor(MediumColor);
                    });
                });
        });
    }

    // ── SECTION 5: TERMS & CONDITIONS ─────────────────────────────────────────

    private void ComposeTermsSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "5",
                    "Terms & Conditions", "الشروط والأحكام"));

            col.Item().Border(1).BorderColor(BorderColor)
                .CornerRadius(4).Padding(12).Column(inner =>
                {
                    var terms = new[]
                    {
                        ("1.",
                         "The Tenant agrees to pay the monthly rent on or " +
                         "before the 5th of each calendar month to the Owner " +
                         "directly, by agreement between the parties.",
                         "يوافق المستأجر على دفع الإيجار الشهري في موعد " +
                         "أقصاه الخامس من كل شهر."),

                        ("2.",
                         "The security deposit shall be refunded within " +
                         "14 days of lease termination, subject to property " +
                         "inspection and deduction of any damages beyond " +
                         "normal wear and tear.",
                         "يُردّ التأمين خلال 14 يوماً من انتهاء العقد " +
                         "بعد الفحص وخصم أي تلفيات."),

                        ("3.",
                         "The Tenant shall not sublet or assign this " +
                         "agreement without the written consent of the Owner.",
                         "لا يحق للمستأجر التأجير من الباطن دون إذن " +
                         "كتابي من المالك."),

                        ("4.",
                         "The Tenant shall maintain the property in good " +
                         "condition and promptly notify the Owner of any " +
                         "maintenance issues.",
                         "يلتزم المستأجر بالحفاظ على العقار وإخطار " +
                         "المالك بأي أعطال."),

                        ("5.",
                         "Either party may terminate this agreement with " +
                         "30 days written notice, except in cases of breach " +
                         "where immediate termination may apply.",
                         "يحق لأي طرف إنهاء العقد بإشعار 30 يوماً " +
                         "كتابياً."),

                        ("6.",
                         "This contract is subject to Egyptian Civil Law " +
                         "and any disputes shall be resolved through " +
                         "competent Egyptian courts.",
                         "يخضع هذا العقد للقانون المدني المصري."),
                    };

                    foreach (var (num, en, ar) in terms)
                    {
                        inner.Item().Row(row =>
                        {
                            row.ConstantItem(20)
                                .Text(num)
                                .FontSize(9).Bold()
                                .FontColor(PrimaryColor);

                            row.RelativeItem().Column(termCol =>
                            {
                                termCol.Item().Text(en)
                                    .FontSize(8).FontColor(DarkColor);

                                termCol.Item().Height(2);

                                termCol.Item().Text(ar)
                                    .FontSize(8)
                                    .FontColor(MediumColor)
                                    .Light();
                            });
                        });

                        if (num != "6.")
                            inner.Item().Height(6);
                    }
                });
        });
    }

    // ── SECTION 6: PLATFORM ROLE ──────────────────────────────────────────────

    private void ComposePlatformSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
                SectionHeader(c, "6",
                    "Platform Role & Disclaimer",
                    "دور المنصة وإخلاء المسؤولية"));

            col.Item().Background(LightColor)
                .Border(1).BorderColor(BorderColor)
                .CornerRadius(4).Padding(12).Column(inner =>
                {
                    inner.Item().Row(row =>
                    {
                        row.ConstantItem(24).Height(24)
                            .Background(PrimaryColor)
                            .CornerRadius(12)
                            .AlignCenter().AlignMiddle()
                            .Text("i")
                            .FontColor(Colors.White)
                            .Bold().FontSize(12);

                        row.ConstantItem(8);

                        row.RelativeItem().AlignMiddle()
                            .Text($"{_settings.CompanyNameEn} Role")
                            .FontSize(9).Bold()
                            .FontColor(DarkColor);
                    });

                    inner.Item().Height(8);

                    inner.Item().Text(
                        $"{_settings.CompanyNameEn} ({_settings.CompanyNameAr}) " +
                        "acts solely as a digital intermediary platform " +
                        "facilitating the connection between property owners " +
                        "and student tenants. Skanly is not a party to this " +
                        "rental agreement and assumes no liability for the " +
                        "condition of the property, payment disputes, or any " +
                        "breach of this contract by either party. " +
                        "The platform commission is charged to the Owner only.")
                        .FontSize(8).FontColor(MediumColor);

                    inner.Item().Height(6);

                    inner.Item().Text(
                        "For support, contact us at " +
                        $"{_settings.SupportEmail} | " +
                        $"{_settings.SupportPhone}")
                        .FontSize(8).FontColor(MediumColor);
                });
        });
    }

    // ── SIGNATURE BLOCK ───────────────────────────────────────────────────────

    private void ComposeSignatureSection(IContainer container)
    {
        container.Border(2).BorderColor(PrimaryColor)
            .CornerRadius(6).Padding(16).Column(col =>
            {
                col.Item().AlignCenter().Text("SIGNATURES / التوقيعات")
                    .FontSize(10).Bold().FontColor(PrimaryColor);

                col.Item().Height(2).Background(PrimaryColor);
                    
                col.Item().Height(12);

                col.Item().Row(row =>
                {
                    // Tenant signature
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Tenant / المستأجر")
                            .FontSize(9).FontColor(MediumColor).Bold();

                        inner.Item().Height(4);
                        inner.Item().Text(_data.StudentFullName)
                            .FontSize(10).Bold().FontColor(DarkColor);

                        inner.Item().Height(2);
                        inner.Item().Text(_data.StudentNationalId)
                            .FontSize(8).FontColor(MediumColor);

                        inner.Item().Height(16);

                        // Digital signature line
                        inner.Item().BorderBottom(1)
                            .BorderColor(DarkColor)
                            .Height(24)
                            .Background("#fafafa")
                            .AlignCenter().AlignMiddle()
                            .Text("[ Digital Confirmation ]")
                            .FontSize(8).FontColor(MediumColor).Light();

                        inner.Item().Height(4);
                        inner.Item().Text(
                            "Digitally confirmed via Skanly on " +
                            _data.GeneratedAt.ToString("yyyy-MM-dd"))
                            .FontSize(7).FontColor(MediumColor);
                    });

                    row.ConstantItem(40).Column(inner =>
                    {
                        inner.Item().Height(40);
                        inner.Item().AlignCenter()
                            .Text("&")
                            .FontSize(22).FontColor(BorderColor);
                    });

                    // Owner signature
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().AlignRight()
                            .Text("Owner / المالك")
                            .FontSize(9).FontColor(MediumColor).Bold();

                        inner.Item().Height(4);
                        inner.Item().AlignRight()
                            .Text(_data.OwnerFullName)
                            .FontSize(10).Bold().FontColor(DarkColor);

                        inner.Item().Height(2);
                        inner.Item().AlignRight()
                            .Text(_data.OwnerNationalId)
                            .FontSize(8).FontColor(MediumColor);

                        inner.Item().Height(16);

                        inner.Item().BorderBottom(1)
                            .BorderColor(DarkColor)
                            .Height(24)
                            .Background("#fafafa")
                            .AlignCenter().AlignMiddle()
                            .Text("[ Awaiting Owner Signature ]")
                            .FontSize(8).FontColor(MediumColor).Light();

                        inner.Item().Height(4);
                        inner.Item().AlignRight()
                            .Text("Owner must sign within 48 hours")
                            .FontSize(7).FontColor(MediumColor);
                    });
                });

                col.Item().Height(12);

                // Platform stamp area
                col.Item().AlignCenter()
                    .Background(LightColor)
                    .Border(1).BorderColor(BorderColor)
                    .CornerRadius(4)
                    .Padding(8).Column(stamp =>
                    {
                        stamp.Item().AlignCenter()
                            .Text("PLATFORM VERIFIED / موثق من المنصة")
                            .FontSize(8).Bold()
                            .FontColor(AccentColor);

                        stamp.Item().Height(2);
                        stamp.Item().AlignCenter()
                            .Text(
                                $"{_settings.CompanyNameEn} · " +
                                $"Contract #{_data.ContractNumber} · " +
                                $"Booking #{_data.BookingId}")
                            .FontSize(7).FontColor(MediumColor);
                    });
            });
    }

    // ── FOOTER ────────────────────────────────────────────────────────────────

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Height(1).Background(BorderColor);
            col.Item().Height(6);

            col.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Contract: ")
                        .FontSize(8).FontColor(MediumColor);
                    text.Span(_data.ContractNumber)
                        .FontSize(8).Bold().FontColor(DarkColor);
                    text.Span($" · Booking: #{_data.BookingId}")
                        .FontSize(8).FontColor(MediumColor);
                });

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span(_settings.CompanyNameEn)
                        .FontSize(8).FontColor(MediumColor);
                    text.Span($" · {_settings.SupportEmail}")
                        .FontSize(8).FontColor(MediumColor);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber()
                        .FontSize(8).FontColor(MediumColor);
                    text.Span(" / ").FontSize(8).FontColor(MediumColor);
                    text.TotalPages().FontSize(8).FontColor(MediumColor);
                });
            });
        });
    }
}