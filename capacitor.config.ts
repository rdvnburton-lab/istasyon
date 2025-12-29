import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.tiginteknoloji.tishift',
  appName: 'Ti-Shift',
  webDir: 'dist/tigin/browser',
  server: {
    androidScheme: 'https',
    // Geliştirme sırasında canlı yenileme için (Kendi IP adresiniz):
    // url: 'http://192.168.1.50:4200',
    // cleartext: true
  },
  plugins: {
    SplashScreen: {
      launchShowDuration: 2000,
      backgroundColor: "#ffffffff",
      showSpinner: true,
      androidScaleType: "CENTER_CROP",
    },
    NativeBiometric: {
      allowDOAuthentication: true // Android cihaz şifresiyle girişe izin ver (Opsiyonel)
    },
    CapacitorHttp: {
      enabled: true,
    }
  }
};

export default config;
