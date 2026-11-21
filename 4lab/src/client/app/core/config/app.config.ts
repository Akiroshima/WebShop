import { registerLocaleData } from '@angular/common';
import ru from '@angular/common/locales/ru';
import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { authInterceptor } from '../interceptors/auth-interceptor';

import { routes } from './app.routes';
import { primeNgRu } from './primeng-ru';
import { MurketPreset } from './theme';

registerLocaleData(ru);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
    ),
    // Реальный REST API бэкенда (/api/v1); при 401 интерцептор обновляет access-cookie.
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    { provide: LOCALE_ID, useValue: 'ru' },
    MessageService,
    ConfirmationService,
    providePrimeNG({
      ripple: true,
      inputVariant: 'outlined',
      translation: primeNgRu,
      theme: {
        preset: MurketPreset,
        options: {
          darkModeSelector: false, // только светлая цветовая схема
          cssLayer: false,
        },
      },
    }),
  ],
};
