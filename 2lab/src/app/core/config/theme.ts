import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * Тема «Муркет»: тёплая кремово-зелёная палитра по макетам.
 * Только светлый режим (darkModeSelector: false задаётся в app.config).
 */
export const MurketPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{green.50}',
      100: '{green.100}',
      200: '{green.200}',
      300: '{green.300}',
      400: '{green.400}',
      500: '{green.600}',
      600: '{green.700}',
      700: '{green.800}',
      800: '{green.900}',
      900: '{green.950}',
      950: '{green.950}',
    },
    colorScheme: {
      light: {
        primary: {
          color: '{green.700}',
          contrastColor: '#ffffff',
          hoverColor: '{green.800}',
          activeColor: '{green.900}',
        },
        highlight: {
          background: '{green.700}',
          focusBackground: '{green.800}',
          color: '#ffffff',
          focusColor: '#ffffff',
        },
        surface: {
          0: '#ffffff',
          50: '#fbfaf6',
          100: '#f6f4ee',
          200: '#efede4',
          300: '#d8d6ca',
          400: '#c4c2b4',
          500: '#a3a193',
          600: '#8a8f7e',
          700: '#6b6f61',
          800: '#4f5247',
          900: '#35372f',
          950: '#1f211b',
        },
        formField: {
          background: '#ffffff',
          borderColor: '#d8d6ca',
          hoverBorderColor: '#8a8f7e',
          focusBorderColor: '{green.700}',
        },
        overlay: {
          select: {
            background: '#ffffff',
            borderColor: '#e0ded4',
          },
        },
      },
      dark: {
        // Тёмная тема отключена (darkModeSelector: false) — секция для совместимости.
        primary: {
          color: '{green.500}',
          contrastColor: '#ffffff',
        },
      },
    },
  },
  tokens: {
    root: {
      'font-family': "'Nunito', 'Segoe UI', system-ui, sans-serif",
    },
  },
});
