// Mock veriler - Gerçek otomasyon verisinden parse edildi
// Tarih: 17.12.2025 Gece Vardiyası

export const MOCK_PERSONELLER = [
    { id: 1, keyId: 'P001', ad: 'A.', soyad: 'VURAL', tamAd: 'A. VURAL', istasyonId: 1, rol: 'POMPACI' as const },
    { id: 2, keyId: 'P002', ad: 'E.', soyad: 'AKCA', tamAd: 'E. AKCA', istasyonId: 1, rol: 'POMPACI' as const },
    { id: 3, keyId: 'P003', ad: 'M.', soyad: 'DUMDUZ', tamAd: 'M. DUMDUZ', istasyonId: 1, rol: 'POMPACI' as const },
    { id: 4, keyId: 'P004', ad: 'F.', soyad: 'BAYLAS', tamAd: 'F. BAYLAS', istasyonId: 1, rol: 'POMPACI' as const },
    { id: 5, keyId: 'V001', ad: 'Vardiya', soyad: 'Sorumlusu', tamAd: 'Vardiya Sorumlusu', istasyonId: 1, rol: 'VARDIYA_SORUMLUSU' as const }
];

// Otomasyon satış verileri - Personel bazlı özetler
export const MOCK_OTOMASYON_OZET = {
    // A.VURAL - Gece vardiyası (00:00 - 07:04)
    'A.VURAL': {
        personelId: 1,
        personelAdi: 'A. VURAL',
        personelKeyId: 'P001',
        satislar: [
            // LPG Satışları
            { yakitTuru: 'LPG', litre: 1221, tutar: 31734, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 1806, tutar: 46938, pompaNo: 13 },
            { yakitTuru: 'LPG', litre: 1539, tutar: 40000, pompaNo: 12 },
            { yakitTuru: 'LPG', litre: 846, tutar: 22000, pompaNo: 13 },
            { yakitTuru: 'LPG', litre: 2501, tutar: 65000, pompaNo: 13 },
            { yakitTuru: 'LPG', litre: 1369, tutar: 35580, pompaNo: 13 },
            { yakitTuru: 'LPG', litre: 2786, tutar: 72408, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 2312, tutar: 60089, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 2152, tutar: 55930, pompaNo: 11 },
            { yakitTuru: 'LPG', litre: 2193, tutar: 57000, pompaNo: 11 },
            { yakitTuru: 'LPG', litre: 751, tutar: 19518, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 1924, tutar: 50000, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 1924, tutar: 50000, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 3639, tutar: 94578, pompaNo: 14 },
            { yakitTuru: 'LPG', litre: 2072, tutar: 53851, pompaNo: 12 },
            { yakitTuru: 'LPG', litre: 1154, tutar: 30000, pompaNo: 13 },
            { yakitTuru: 'LPG', litre: 2210, tutar: 57438, pompaNo: 12 },
            { yakitTuru: 'LPG', litre: 1924, tutar: 50000, pompaNo: 11 },
            { yakitTuru: 'LPG', litre: 3389, tutar: 88080, pompaNo: 14 },
            // ... daha fazla LPG
            // Motorin Satışları  
            { yakitTuru: 'MOTORIN', litre: 2659, tutar: 144000, pompaNo: 3 },
            { yakitTuru: 'MOTORIN', litre: 1846, tutar: 100000, pompaNo: 7 },
            { yakitTuru: 'MOTORIN', litre: 1477, tutar: 80000, pompaNo: 1 },
            { yakitTuru: 'MOTORIN', litre: 5373, tutar: 291000, pompaNo: 4 },
            { yakitTuru: 'MOTORIN', litre: 2770, tutar: 150000, pompaNo: 3 },
            { yakitTuru: 'MOTORIN', litre: 923, tutar: 50000, pompaNo: 7 },
            { yakitTuru: 'MOTORIN', litre: 1662, tutar: 90000, pompaNo: 3 },
            { yakitTuru: 'MOTORIN', litre: 554, tutar: 30000, pompaNo: 7 },
            { yakitTuru: 'MOTORIN', litre: 4985, tutar: 270000, pompaNo: 5 },
            { yakitTuru: 'MOTORIN', litre: 4247, tutar: 230000, pompaNo: 3 },
            { yakitTuru: 'MOTORIN', litre: 3508, tutar: 190000, pompaNo: 6 },
            { yakitTuru: 'MOTORIN', litre: 923, tutar: 50000, pompaNo: 1 },
            { yakitTuru: 'MOTORIN', litre: 3693, tutar: 200000, pompaNo: 3 },
            // Kurşunsuz Satışları
            { yakitTuru: 'KURSUNSUZ', litre: 951, tutar: 50000, pompaNo: 4 },
            { yakitTuru: 'KURSUNSUZ', litre: 761, tutar: 40000, pompaNo: 5 },
            { yakitTuru: 'KURSUNSUZ', litre: 4185, tutar: 220000, pompaNo: 7 },
            { yakitTuru: 'KURSUNSUZ', litre: 666, tutar: 35000, pompaNo: 7 },
            { yakitTuru: 'KURSUNSUZ', litre: 951, tutar: 50000, pompaNo: 7 },
            { yakitTuru: 'KURSUNSUZ', litre: 380, tutar: 20000, pompaNo: 1 },
            { yakitTuru: 'KURSUNSUZ', litre: 1427, tutar: 75000, pompaNo: 7 },
            { yakitTuru: 'KURSUNSUZ', litre: 1902, tutar: 100000, pompaNo: 8 },
            { yakitTuru: 'KURSUNSUZ', litre: 1141, tutar: 60000, pompaNo: 3 }
        ],
        toplamlar: {
            LPG: { litre: 78254, tutar: 2033695 },
            MOTORIN: { litre: 34620, tutar: 1875000 },
            KURSUNSUZ: { litre: 12364, tutar: 650000 }
        },
        genelToplam: 4558695 // 45.586,95 TL (kuruş cinsinden)
    },
    // E.AKCA
    'E.AKCA': {
        personelId: 2,
        personelAdi: 'E. AKCA',
        personelKeyId: 'P002',
        toplamlar: {
            LPG: { litre: 9909, tutar: 257532 },
            KURSUNSUZ: { litre: 14810, tutar: 778571 },
            MOTORIN: { litre: 0, tutar: 0 }
        },
        genelToplam: 1036103
    },
    // M.DUMDUZ  
    'M.DUMDUZ': {
        personelId: 3,
        personelAdi: 'M. DUMDUZ',
        personelKeyId: 'P003',
        toplamlar: {
            LPG: { litre: 12927, tutar: 335985 },
            MOTORIN: { litre: 6795, tutar: 368000 },
            KURSUNSUZ: { litre: 0, tutar: 0 }
        },
        genelToplam: 703985
    },
    // F.BAYLAS
    'F.BAYLAS': {
        personelId: 4,
        personelAdi: 'F. BAYLAS',
        personelKeyId: 'P004',
        toplamlar: {
            LPG: { litre: 14803, tutar: 384728 },
            MOTORIN: { litre: 369, tutar: 20000 },
            KURSUNSUZ: { litre: 0, tutar: 0 }
        },
        genelToplam: 404728
    }
};

// Filo satışları (Şirket araçları - Veresiye)
export const MOCK_FILO_SATISLARI = [
    { filoAdi: '', plaka: '06DKG804', yakitTuru: 'KURSUNSUZ', litre: 4128, tutar: 217000, fisNo: 242436 },
    { filoAdi: '', plaka: '06DTR975', yakitTuru: 'MOTORIN', litre: 5890, tutar: 319000, fisNo: 242451 },
    { filoAdi: '', plaka: '06FJS032', yakitTuru: 'MOTORIN', litre: 3840, tutar: 208000, fisNo: 242460 },
    { filoAdi: '', plaka: '06ECC499', yakitTuru: 'MOTORIN', litre: 4985, tutar: 270000, fisNo: 242485 },
    { filoAdi: '', plaka: '06FLM894', yakitTuru: 'MOTORIN', litre: 5130, tutar: 277841, fisNo: 242495 },
    { filoAdi: '', plaka: '06ECK766', yakitTuru: 'MOTORIN', litre: 6117, tutar: 331297, fisNo: 242499 },
    { filoAdi: '', plaka: '034HLM70', yakitTuru: 'MOTORIN', litre: 5937, tutar: 321548, fisNo: 242501 },
    { filoAdi: 'OTOBILIM', plaka: '34DPC401', yakitTuru: 'KURSUNSUZ', litre: 3133, tutar: 164702, fisNo: 242510 },
    { filoAdi: '', plaka: '34KVF989', yakitTuru: 'MOTORIN', litre: 4623, tutar: 250382, fisNo: 242511 },
    { filoAdi: '', plaka: '06DDR448', yakitTuru: 'KURSUNSUZ', litre: 4246, tutar: 223212, fisNo: 242512 },
    { filoAdi: '', plaka: '06DKB582', yakitTuru: 'MOTORIN', litre: 4219, tutar: 228501, fisNo: 242507 },
    { filoAdi: '', plaka: '06DBS424', yakitTuru: 'MOTORIN', litre: 2030, tutar: 109945, fisNo: 242508 }
];

// Vardiya özet bilgileri
export const MOCK_VARDIYA_OZET = {
    tarih: '17.12.2025',
    baslangic: '00:00:00',
    bitis: '07:34:25',
    istasyonAdi: 'Shell Merkez',
    toplamIslemSayisi: 100,

    // Yakıt bazlı toplamlar
    yakitToplamlar: {
        LPG: { litre: 115893, tutar: 3011940, birimFiyat: 25.99 },
        MOTORIN: { litre: 89766, tutar: 4862273, birimFiyat: 54.16 },
        KURSUNSUZ: { litre: 38428, tutar: 2020485, birimFiyat: 52.57 }
    },

    // Personel bazlı toplamlar (Kuruş cinsinden)
    personelToplamlar: {
        'A.VURAL': 4558695,
        'E.AKCA': 1036103,
        'M.DUMDUZ': 703985,
        'F.BAYLAS': 404728
    },

    // Filo (Veresiye) toplamı
    filoToplam: 2921428,

    // Genel toplam
    genelToplam: 9624939 // 96.249,39 TL
};
